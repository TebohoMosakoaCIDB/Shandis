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
    private const string UserEmailKey = "shandis.user-email";

    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private bool isInitialized;
    private string? refreshToken;
    private DateTimeOffset? accessTokenExpiresAt;

    public bool IsAuthenticated { get; private set; }
    public string? CurrentUserEmail { get; private set; }

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

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = $"Login failed. Status: {(int)response.StatusCode} " +
                        $"{response.StatusCode}. Response: {responseBody}";

            return (false, error);
        }

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
        CurrentUserEmail = email.Trim();
        await js.InvokeVoidAsync("shandisAuth.set", UserEmailKey, CurrentUserEmail);
        isInitialized = true;
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(
        string userName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await apiClient.HttpClient.PostAsJsonAsync(
            "api/accounts/register",
            new { userName, email, password },
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
        CurrentUserEmail = await js.InvokeAsync<string?>("shandisAuth.get", UserEmailKey);
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
        CurrentUserEmail = null;
        await js.InvokeVoidAsync("shandisAuth.remove", UserEmailKey);
        IsAuthenticated = false;
        isInitialized = true;
    }

    private async Task EnsureValidAccessTokenAsync(CancellationToken cancellationToken)
    {
        await InitializeAsync();

        if (!IsAuthenticated)
            throw new UnauthorizedAccessException("You are not signed in. Please sign in again.");

        if (accessTokenExpiresAt is not null &&
            accessTokenExpiresAt.Value > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return;
        }

        await refreshLock.WaitAsync(cancellationToken);
        try
        {
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

            if (document.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? body;

            if (document.RootElement.TryGetProperty("errors", out var errorsElement) &&
                errorsElement.ValueKind == JsonValueKind.Array)
            {
                return string.Join(" ", errorsElement.EnumerateArray().Select(x => x.GetString()));
            }
        }
        catch (JsonException)
        {
        }

        return body;
    }
}