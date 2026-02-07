using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ConferenceHub.Models;
using Microsoft.Extensions.Options;

namespace ConferenceHub.Services
{
    public interface ISlideStorageService
    {
        Task<List<string>> UploadSlidesAsync(int sessionId, IEnumerable<IFormFile> files);
    }

    public class SlideStorageService : ISlideStorageService
    {
        private readonly SlideStorageConfig _config;

        public SlideStorageService(IOptions<SlideStorageConfig> config)
        {
            _config = config.Value;
        }

        public async Task<List<string>> UploadSlidesAsync(int sessionId, IEnumerable<IFormFile> files)
        {
            if (string.IsNullOrWhiteSpace(_config.ConnectionString))
            {
                throw new InvalidOperationException("Slide storage connection string is not configured.");
            }

            var blobServiceClient = new BlobServiceClient(_config.ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_config.ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var uploadedUrls = new List<string>();

            foreach (var file in files.Where(f => f.Length > 0))
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var blobName = $"sessions/{sessionId}/{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension}";
                var blobClient = containerClient.GetBlobClient(blobName);

                await using var stream = file.OpenReadStream();
                var headers = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType
                };

                await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers });
                uploadedUrls.Add(blobClient.Uri.ToString());
            }

            return uploadedUrls;
        }
    }
}
