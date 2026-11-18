using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
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


    public async Task<CreatedMemoryResponse> CreateMemoryAsync(
        CreateMemoryRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.PostAsJsonAsync(
            "api/memories",
            request,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<CreatedMemoryResponse>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(
                "The Shandis API returned an empty Memory response.");
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

    public async Task<MomentItem> CreateMomentAsync(
        Guid memoryId,
        int momentType,
        string? title,
        string? content,
        IReadOnlyCollection<IBrowserFile> media,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(memoryId.ToString()), "MemoryId");
        form.Add(new StringContent(momentType.ToString()), "MomentType");

        if (!string.IsNullOrWhiteSpace(title))
            form.Add(new StringContent(title.Trim()), "Title");

        if (!string.IsNullOrWhiteSpace(content))
            form.Add(new StringContent(content.Trim()), "Content");

        foreach (var file in media)
        {
            var stream = file.OpenReadStream(
                maxAllowedSize: 50 * 1024 * 1024,
                cancellationToken: cancellationToken);

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(file.ContentType);

            form.Add(fileContent, "Media", file.Name);
        }

        using var response = await HttpClient.PostAsync(
            "api/moments",
            form,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<MomentItem>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(
                "The Shandis API returned an empty Moment response.");
    }

    public async Task<MomentLikeResult> LikeMomentAsync(
        Guid momentId,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.PostAsync(
            $"api/moments/{momentId}/like",
            content: null,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<MomentLikeResult>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The Shandis API returned an empty like response.");
    }

    public async Task<MomentLikeResult> UnlikeMomentAsync(
        Guid momentId,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.DeleteAsync(
            $"api/moments/{momentId}/like",
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<MomentLikeResult>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The Shandis API returned an empty unlike response.");
    }

    public async Task<MomentCommentsResponse> GetMomentCommentsAsync(
        Guid momentId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<MomentCommentsResponse>(
            $"api/moments/{momentId}/comments?page={page}&pageSize={pageSize}",
            cancellationToken);
    }

    public async Task<MomentCommentItem> CreateMomentCommentAsync(
        Guid momentId,
        string content,
        Guid? parentCommentId = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.PostAsJsonAsync(
            $"api/moments/{momentId}/comments",
            new CreateMomentCommentRequest
            {
                Content = content,
                ParentCommentId = parentCommentId
            },
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<MomentCommentItem>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The Shandis API returned an empty comment response.");
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
