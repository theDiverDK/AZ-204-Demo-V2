using System.Text.Json;
using Azure.Storage.Queues;
using ConferenceHub.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ConferenceHub.Services
{
    public interface IThumbnailJobQueueService
    {
        Task EnqueueAsync(int sessionId, IEnumerable<string> slideUrls);
    }

    public class ThumbnailJobQueueService : IThumbnailJobQueueService
    {
        private readonly ThumbnailQueueConfig _config;
        private readonly TelemetryClient _telemetryClient;
        private readonly IKeyVaultTelemetryService _keyVaultTelemetryService;
        private readonly ILogger<ThumbnailJobQueueService> _logger;

        public ThumbnailJobQueueService(
            IOptions<ThumbnailQueueConfig> config,
            TelemetryClient telemetryClient,
            IKeyVaultTelemetryService keyVaultTelemetryService,
            ILogger<ThumbnailJobQueueService> logger)
        {
            _config = config.Value;
            _telemetryClient = telemetryClient;
            _keyVaultTelemetryService = keyVaultTelemetryService;
            _logger = logger;
        }

        public async Task EnqueueAsync(int sessionId, IEnumerable<string> slideUrls)
        {
            if (string.IsNullOrWhiteSpace(_config.ConnectionString) || string.IsNullOrWhiteSpace(_config.QueueName))
            {
                _logger.LogInformation("Thumbnail queue is not configured. Skipping thumbnail jobs.");
                return;
            }

            await _keyVaultTelemetryService.ProbeAsync("QueueEnqueue");

            var queue = new QueueClient(_config.ConnectionString, _config.QueueName);
            await queue.CreateIfNotExistsAsync();

            foreach (var slideUrl in slideUrls)
            {
                var payload = JsonSerializer.Serialize(new
                {
                    sessionId,
                    slideUrl,
                    traceparent = Activity.Current?.Id
                });

                var startTime = DateTimeOffset.UtcNow;
                var sw = Stopwatch.StartNew();
                await queue.SendMessageAsync(payload);
                _telemetryClient.TrackDependency("Azure Storage Queue", _config.QueueName, "Enqueue", "ThumbnailJob", startTime, sw.Elapsed, "OK", true);
                _telemetryClient.TrackEvent("ThumbnailJobEnqueued", new Dictionary<string, string>
                {
                    ["Queue"] = _config.QueueName,
                    ["SessionId"] = sessionId.ToString(),
                    ["SlideUrl"] = slideUrl
                });
            }
        }
    }
}
