using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;

namespace ConferenceHub.Functions;

public sealed class ThumbnailQueueFunction
{
    private readonly ILogger<ThumbnailQueueFunction> _logger;

    public ThumbnailQueueFunction(ILogger<ThumbnailQueueFunction> logger)
    {
        _logger = logger;
    }

    [Function("ProcessSlideThumbnailJob")]
    public async Task Run([QueueTrigger("%ThumbnailQueueName%", Connection = "ThumbnailQueueConnection")] string queueMessage)
    {
        try
        {
            _logger.LogInformation("QUEUE_MESSAGE_RECEIVED");
            _logger.LogInformation("Processing thumbnail queue message: {QueueMessage}", queueMessage);

            var job = DeserializePayload(queueMessage);

            if (job == null || string.IsNullOrWhiteSpace(job.SlideUrl))
            {
                throw new InvalidOperationException("Thumbnail queue message is empty or invalid.");
            }

            using var activity = StartConsumerActivity("ProcessSlideThumbnailJob", job.TraceParent);

            var extension = Path.GetExtension(job.SlideUrl).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
            {
                _logger.LogInformation("Skipping thumbnail generation for non-image slide: {SlideUrl}", job.SlideUrl);
                return;
            }

            var slidesConnectionString = Environment.GetEnvironmentVariable("SlidesStorageConnectionString") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(slidesConnectionString))
            {
                // Fallback: queue connection points to the same storage account in this learning path.
                slidesConnectionString = Environment.GetEnvironmentVariable("ThumbnailQueueConnection") ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(slidesConnectionString))
            {
                throw new InvalidOperationException("SlidesStorageConnectionString is not configured.");
            }

            var slideUri = new Uri(job.SlideUrl);
            var segments = slideUri.Segments;
            if (segments.Length < 3)
            {
                throw new InvalidOperationException($"Slide URL does not contain expected container/blob path: {job.SlideUrl}");
            }

            var containerName = segments[1].Trim('/');
            var blobName = Uri.UnescapeDataString(string.Concat(segments.Skip(2)));
            _logger.LogInformation("Creating thumbnail from container '{Container}' blob '{BlobName}'", containerName, blobName);
            var sourceBlob = new BlobContainerClient(slidesConnectionString, containerName).GetBlobClient(blobName);

            if (!await sourceBlob.ExistsAsync())
            {
                throw new InvalidOperationException($"Source blob does not exist: {containerName}/{blobName}");
            }

            var fileNameWithoutExtension = blobName.Substring(0, blobName.LastIndexOf('.'));
            var thumbnailBlobName = fileNameWithoutExtension + "-thumb.jpg";
            var thumbnailBlob = new BlobContainerClient(slidesConnectionString, containerName).GetBlobClient(thumbnailBlobName);

            await using var sourceStream = new MemoryStream();
            await sourceBlob.DownloadToAsync(sourceStream);
            sourceStream.Position = 0;

            using var image = await Image.LoadAsync(sourceStream);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(320, 200),
                Mode = ResizeMode.Max
            }));

            await using var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 80 });
            outputStream.Position = 0;

            await thumbnailBlob.DeleteIfExistsAsync();
            await thumbnailBlob.UploadAsync(outputStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
            });
            _logger.LogInformation("Generated thumbnail for slide {SlideUrl}", job.SlideUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thumbnail generation failed for queue message.");
            throw;
        }
    }

    private static ThumbnailJobPayload? DeserializePayload(string rawMessage)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            return JsonSerializer.Deserialize<ThumbnailJobPayload>(rawMessage, options);
        }
        catch
        {
        }

        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(rawMessage));
            return JsonSerializer.Deserialize<ThumbnailJobPayload>(decoded, options);
        }
        catch
        {
            return null;
        }
    }

    private sealed class ThumbnailJobPayload
    {
        public int SessionId { get; set; }
        public string? SlideUrl { get; set; }
        public string? TraceParent { get; set; }
    }

    private static Activity? StartConsumerActivity(string name, string? traceParent)
    {
        var activity = new Activity(name);
        if (!string.IsNullOrWhiteSpace(traceParent))
        {
            activity.SetParentId(traceParent);
        }

        return activity.Start();
    }
}
