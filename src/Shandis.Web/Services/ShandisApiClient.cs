using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shandis.Web.Models;

namespace Shandis.Web.Services;

public sealed class ShandisApiClient(HttpClient httpClient)
{
    public HttpClient HttpClient { get; } = httpClient;

    public void SetAccessToken(string? accessToken)
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(accessToken)
                ? null
                : new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<MyMemoriesResponse> GetMyMemoriesAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<MyMemoriesResponse>(
            $"api/memories/mine?page={page}&pageSize={pageSize}",
            cancellationToken);
    }

    public async Task<SharedMemoriesResponse> GetSharedMemoriesAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<SharedMemoriesResponse>(
            $"api/memories/shared-with-me?page={page}&pageSize={pageSize}",
            cancellationToken);
    }

    public async Task<MemoryDetails?> GetMemoryAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(
            $"api/memories/{memoryId}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<MemoryDetails>(
            cancellationToken: cancellationToken);
    }

    public async Task<GetMomentsResponse> GetMomentsAsync(
        Guid memoryId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<GetMomentsResponse>(
            $"api/moments/{memoryId}/moments?page={page}&pageSize={pageSize}",
            cancellationToken);
    }

    private async Task<T> GetRequiredAsync<T>(
        string requestUri,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(requestUri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<T>(
            cancellationToken: cancellationToken);

        return result ?? throw new InvalidOperationException(
            "The Shandis API returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        throw new HttpRequestException(
            string.IsNullOrWhiteSpace(body)
                ? $"The Shandis API returned {(int)response.StatusCode} ({response.ReasonPhrase})."
                : body,
            inner: null,
            response.StatusCode);
    }
}
