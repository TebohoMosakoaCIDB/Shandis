using System.Text;
using Shandis.Infrastructure.Options;
using Shandis.Infrastructure.Services.Storage;
using Microsoft.Extensions.Options;

namespace Shandis.IntegrationTests.Storage;

public sealed class SupabaseFactAttribute : FactAttribute
{
    public SupabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("SHANDIS_SUPABASE_URL")) ||
            string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("SHANDIS_SUPABASE_SECRET_KEY")) ||
            string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("SHANDIS_SUPABASE_BUCKET")))
        {
            Skip = "Supabase integration credentials are not configured.";
        }
    }
}

public class SupabaseMediaStorageTests
{
    [Fact]
    [Trait("Category", "Supabase")]
    public async Task StorageLifecycle_ShouldUploadDownloadAndDeleteFile()
    {
        var supabaseUrl =
            Environment.GetEnvironmentVariable("SHANDIS_SUPABASE_URL");

        var secretKey =
            Environment.GetEnvironmentVariable("SHANDIS_SUPABASE_SECRET_KEY");

        var bucketName =
            Environment.GetEnvironmentVariable("SHANDIS_SUPABASE_BUCKET");

        if (string.IsNullOrWhiteSpace(supabaseUrl) ||
            string.IsNullOrWhiteSpace(secretKey) ||
            string.IsNullOrWhiteSpace(bucketName))
        {
            throw new InvalidOperationException(
                "Set SHANDIS_SUPABASE_URL, " +
                "SHANDIS_SUPABASE_SECRET_KEY and " +
                "SHANDIS_SUPABASE_BUCKET environment variables " +
                "before running this integration test.");
        }

        var options = Options.Create(new SupabaseStorageOptions
        {
            Url = supabaseUrl,
            SecretKey = secretKey,
            BucketName = bucketName
        });

        using var storageHttpClient = new HttpClient();

        var storageService = new SupabaseMediaStorageService(
            storageHttpClient,
            options);

        using var downloadHttpClient = new HttpClient();

        var expectedContent =
            $"Shandis Supabase integration test: {Guid.NewGuid()}";

        var objectPath =
            $"integration-tests/{Guid.NewGuid():N}/test.txt";

        var uploaded = false;

        try
        {
            // Arrange
            await using var uploadStream = new MemoryStream(
                Encoding.UTF8.GetBytes(expectedContent));

            // Act: Upload
            await storageService.UploadAsync(
                objectPath,
                uploadStream,
                "text/plain");

            uploaded = true;

            // Act: Generate signed URL
            var signedUrl = await storageService.CreateSignedUrlAsync(
                objectPath,
                300);

            Assert.False(string.IsNullOrWhiteSpace(signedUrl));

            // Act: Download
            using var response = await downloadHttpClient.GetAsync(signedUrl);

            response.EnsureSuccessStatusCode();

            var actualContent = await response.Content.ReadAsStringAsync();

            // Assert: Content matches
            Assert.Equal(expectedContent, actualContent);

            // Act: Delete
            await storageService.DeleteAsync(objectPath);

            uploaded = false;
        }
        finally
        {
            // Clean up if an earlier assertion or operation failed.
            if (uploaded)
            {
                await storageService.DeleteAsync(objectPath);
            }
        }
    }
}
