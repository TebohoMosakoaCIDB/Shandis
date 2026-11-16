
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Shandis.Application.Abstractions.Storage;
using Shandis.Infrastructure.Options;

namespace Shandis.Infrastructure.Services.Storage;

public sealed class SupabaseMediaStorageService : IMediaStorageService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseStorageOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SupabaseMediaStorageService(
        HttpClient httpClient,
        IOptions<SupabaseStorageOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.DefaultRequestHeaders.Remove("apikey");
        _httpClient.DefaultRequestHeaders.Add(
            "apikey",
            _options.SecretKey);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _options.SecretKey);
    }

    public async Task UploadAsync(
        string objectPath,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        if (!fileStream.CanRead)
            throw new ArgumentException("The file stream must be readable.");

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.");

        var url = BuildObjectUrl(objectPath);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        var content = new StreamContent(fileStream);
        content.Headers.ContentType =
            MediaTypeHeaderValue.Parse(contentType);

        request.Content = content;
        request.Headers.Add("x-upsert", "false");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteAsync(
        string objectPath,
        CancellationToken cancellationToken = default)
    {
        ValidateObjectPath(objectPath);

        var url =
            $"{GetStorageBaseUrl()}/object/{EscapeSegment(_options.BucketName)}";

        var payload = JsonSerializer.Serialize(new
        {
            prefixes = new[] { objectPath }
        });

        using var request = new HttpRequestMessage(HttpMethod.Delete, url)
        {
            Content = new StringContent(
                payload,
                Encoding.UTF8,
                "application/json")
        };

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public string GetPublicUrl(string objectPath)
    {
        ValidateObjectPath(objectPath);

        return
            $"{GetStorageBaseUrl()}/object/public/" +
            $"{EscapeSegment(_options.BucketName)}/" +
            EscapeObjectPath(objectPath);
    }

    public async Task<string> CreateSignedUrlAsync(
        string objectPath,
        int expiresInSeconds = 3600,
        CancellationToken cancellationToken = default)
    {
        ValidateObjectPath(objectPath);

        if (expiresInSeconds <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(expiresInSeconds));

        var url =
            $"{GetStorageBaseUrl()}/object/sign/" +
            $"{EscapeSegment(_options.BucketName)}/" +
            EscapeObjectPath(objectPath);

        var payload = JsonSerializer.Serialize(new
        {
            expiresIn = expiresInSeconds
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                payload,
                Encoding.UTF8,
                "application/json")
        };

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

        var result = JsonSerializer.Deserialize<SignedUrlResponse>(
            json,
            JsonOptions);

        if (string.IsNullOrWhiteSpace(result?.SignedURL))
            throw new InvalidOperationException(
                "Supabase did not return a signed URL.");

        return Uri.TryCreate(
            result.SignedURL,
            UriKind.Absolute,
            out var absoluteUri)
                ? absoluteUri.ToString()
                : $"{_options.Url.TrimEnd('/')}/storage/v1" +
                  result.SignedURL;
    }

    private string GetStorageBaseUrl()
    {
        return $"{_options.Url.TrimEnd('/')}/storage/v1";
    }

    private string BuildObjectUrl(string objectPath)
    {
        ValidateObjectPath(objectPath);

        return
            $"{GetStorageBaseUrl()}/object/" +
            $"{EscapeSegment(_options.BucketName)}/" +
            EscapeObjectPath(objectPath);
    }

    private static string EscapeObjectPath(string objectPath)
    {
        return string.Join(
            "/",
            objectPath.Split('/').Select(EscapeSegment));
    }

    private static string EscapeSegment(string value)
    {
        return Uri.EscapeDataString(value);
    }

    private static void ValidateObjectPath(string objectPath)
    {
        if (string.IsNullOrWhiteSpace(objectPath))
            throw new ArgumentException("Object path is required.");

        if (objectPath.StartsWith('/') ||
            objectPath.Contains('\\') ||
            objectPath.Split('/').Any(
                segment => string.IsNullOrWhiteSpace(segment) ||
                           segment is "." or ".."))
        {
            throw new ArgumentException("Invalid storage object path.");
        }
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var error = await response.Content.ReadAsStringAsync(
            cancellationToken);

        throw new HttpRequestException(
            $"Supabase Storage returned {(int)response.StatusCode} " +
            $"{response.ReasonPhrase}: {error}",
            null,
            response.StatusCode);
    }

    private sealed class SignedUrlResponse
    {
        public string SignedURL { get; set; } = string.Empty;
    }
}
