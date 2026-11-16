
namespace Shandis.Application.Abstractions.Storage;

public interface IMediaStorageService
{
    Task UploadAsync(
        string objectPath,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectPath,
        CancellationToken cancellationToken = default);

    string GetPublicUrl(string objectPath);

    Task<string> CreateSignedUrlAsync(
        string objectPath,
        int expiresInSeconds = 3600,
        CancellationToken cancellationToken = default);
}
