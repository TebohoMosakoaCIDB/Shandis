using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Shandis.Web.Services;

public sealed class ShandisAuthService
{
    private readonly ShandisApiClient apiClient;
    private readonly IJSRuntime js;
    private const string AccessTokenKey = "shandis.access-token";
    private const string RefreshTokenKey = "shandis.refresh-token";
    private const string AccessTokenExpiresAtKey = "shandis.access-token-expires-at";

    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private bool isInitialized;
    private string? refreshToken;
    private DateTimeOffset? accessTokenExpiresAt;

    public bool IsAuthenticated { get; private set; }

    // Register the token check once. ShandisApiClient invokes it before protected API calls.
    public ShandisAuthService(ShandisApiClient apiClient, IJSRuntime js)
    {
        this.apiClient = apiClient;
        this.js = js;
        apiClient.SetBeforeRequest(EnsureValidAccessTokenAsync);
    }

    public async Task<(bool Success, string? Error)> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.HttpClient.PostAsJsonAsync(
            "api/auth/login?useCookies=false&useSessionCookies=false",
            new { email, password },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return (false, await ReadErrorAsync(response));

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        var tokens = ReadTokens(document.RootElement);
        if (string.IsNullOrWhiteSpace(tokens.AccessToken) ||
            string.IsNullOrWhiteSpace(tokens.RefreshToken))
        {
            return (false, "The API did not return both access and refresh tokens. Please check the API authentication configuration.");
        }

        await StoreTokensAsync(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);
        isInitialized = true;
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.HttpClient.PostAsJsonAsync(
            "api/auth/register",
            new { email, password },
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ReadErrorAsync(response));
    }

    public async Task InitializeAsync()
    {
        if (isInitialized)
            return;

        var accessToken = await js.InvokeAsync<string?>("shandisAuth.get", AccessTokenKey);
        refreshToken = await js.InvokeAsync<string?>("shandisAuth.get", RefreshTokenKey);

        var expiresAt = await js.InvokeAsync<string?>("shandisAuth.get", AccessTokenExpiresAtKey);
        if (DateTimeOffset.TryParse(expiresAt, out var parsedExpiry))
            accessTokenExpiresAt = parsedExpiry;

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            apiClient.SetAccessToken(accessToken);
            IsAuthenticated = true;
        }

        isInitialized = true;
    }

    public async Task LogoutAsync()
    {
        await ClearStoredTokensAsync();
        apiClient.SetAccessToken(null);
        refreshToken = null;
        accessTokenExpiresAt = null;
        IsAuthenticated = false;
        isInitialized = true;
    }

    private async Task EnsureValidAccessTokenAsync(CancellationToken cancellationToken)
    {
        await InitializeAsync();

        if (!IsAuthenticated)
            throw new UnauthorizedAccessException("You are not signed in. Please sign in again.");

        // Refresh shortly before expiry to avoid a token expiring during an API request.
        if (accessTokenExpiresAt is null ||
            accessTokenExpiresAt.Value > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return;
        }

        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Another request may have refreshed while this request waited.
            if (accessTokenExpiresAt is not null &&
                accessTokenExpiresAt.Value > DateTimeOffset.UtcNow.AddMinutes(2))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                await LogoutAsync();
                throw new UnauthorizedAccessException("Your session has expired. Please sign in again.");
            }

            using var response = await apiClient.HttpClient.PostAsJsonAsync(
                "api/auth/refresh",
                new { refreshToken },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await LogoutAsync();
                throw new UnauthorizedAccessException("Your session has expired. Please sign in again.");
            }

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            var tokens = ReadTokens(document.RootElement);
            if (string.IsNullOrWhiteSpace(tokens.AccessToken) ||
                string.IsNullOrWhiteSpace(tokens.RefreshToken))
            {
                await LogoutAsync();
                throw new UnauthorizedAccessException("Your session could not be renewed. Please sign in again.");
            }

            await StoreTokensAsync(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private async Task StoreTokensAsync(
        string accessToken,
        string refreshTokenValue,
        int expiresIn)
    {
        refreshToken = refreshTokenValue;
        accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

        await js.InvokeVoidAsync("shandisAuth.set", AccessTokenKey, accessToken);
        await js.InvokeVoidAsync("shandisAuth.set", RefreshTokenKey, refreshToken);
        await js.InvokeVoidAsync(
            "shandisAuth.set",
            AccessTokenExpiresAtKey,
            accessTokenExpiresAt.Value.ToString("O"));

        apiClient.SetAccessToken(accessToken);
        IsAuthenticated = true;
    }

    private async Task ClearStoredTokensAsync()
    {
        await js.InvokeVoidAsync("shandisAuth.remove", AccessTokenKey);
        await js.InvokeVoidAsync("shandisAuth.remove", RefreshTokenKey);
        await js.InvokeVoidAsync("shandisAuth.remove", AccessTokenExpiresAtKey);
    }

    private static (string? AccessToken, string? RefreshToken, int ExpiresIn) ReadTokens(
        JsonElement root)
    {
        var accessToken = root.TryGetProperty("accessToken", out var access)
            ? access.GetString()
            : null;
        var refreshToken = root.TryGetProperty("refreshToken", out var refresh)
            ? refresh.GetString()
            : null;
        var expiresIn = root.TryGetProperty("expiresIn", out var expires) &&
                        expires.TryGetInt32(out var seconds)
            ? seconds
            : 3600;

        return (accessToken, refreshToken, expiresIn);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(body))
            return $"Authentication failed ({(int)response.StatusCode}).";

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                var errors = document.RootElement.EnumerateArray()
                    .Select(item => item.TryGetProperty("description", out var description)
                        ? description.GetString()
                        : item.ToString())
                    .Where(value => !string.IsNullOrWhiteSpace(value));

                return string.Join(" ", errors);
            }

            if (document.RootElement.TryGetProperty("title", out var title))
                return title.GetString() ?? body;

            if (document.RootElement.TryGetProperty("detail", out var detail))
                return detail.GetString() ?? body;
        }
        catch (JsonException)
        {
            // Return the raw response when it isn't JSON.
        }

        return body;
    }
}