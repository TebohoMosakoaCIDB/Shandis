using System.Net.Http.Headers;

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
}