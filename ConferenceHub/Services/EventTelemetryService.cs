using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using ConferenceHub.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ConferenceHub.Services
{
    public interface IEventTelemetryService
    {
        Task TrackAsync(string eventName, object payload);
    }

    public class EventTelemetryService : IEventTelemetryService
    {
        private readonly EventHubConfig _config;
        private readonly TelemetryClient _telemetryClient;
        private readonly IKeyVaultTelemetryService _keyVaultTelemetryService;
        private readonly ILogger<EventTelemetryService> _logger;

        public EventTelemetryService(
            IOptions<EventHubConfig> config,
            TelemetryClient telemetryClient,
            IKeyVaultTelemetryService keyVaultTelemetryService,
            ILogger<EventTelemetryService> logger)
        {
            _config = config.Value;
            _telemetryClient = telemetryClient;
            _keyVaultTelemetryService = keyVaultTelemetryService;
            _logger = logger;
        }

        public async Task TrackAsync(string eventName, object payload)
        {
            if (string.IsNullOrWhiteSpace(_config.ConnectionString) || string.IsNullOrWhiteSpace(_config.HubName))
            {
                _logger.LogInformation("EventHub telemetry is not configured. Skipping event {EventName}", eventName);
                return;
            }

            try
            {
                await _keyVaultTelemetryService.ProbeAsync("EventHubPublish");

                await using var producer = new EventHubProducerClient(_config.ConnectionString, _config.HubName);
                using var batch = await producer.CreateBatchAsync();
                var startTime = DateTimeOffset.UtcNow;
                var sw = Stopwatch.StartNew();

                var envelope = new
                {
                    eventName,
                    timestampUtc = DateTime.UtcNow,
                    payload
                };

                var message = JsonSerializer.Serialize(envelope);
                if (!batch.TryAdd(new EventData(message)))
                {
                    _logger.LogWarning("Event payload too large for Event Hub batch: {EventName}", eventName);
                    return;
                }

                await producer.SendAsync(batch);
                _telemetryClient.TrackDependency("Azure Event Hubs", _config.HubName, "Publish", eventName, startTime, sw.Elapsed, "OK", true);
                _telemetryClient.TrackEvent("EventHubMessagePublished", new Dictionary<string, string>
                {
                    ["EventName"] = eventName,
                    ["HubName"] = _config.HubName
                });
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackEvent("EventHubMessagePublishFailed", new Dictionary<string, string>
                {
                    ["EventName"] = eventName,
                    ["HubName"] = _config.HubName,
                    ["Error"] = ex.Message
                });
                _logger.LogError(ex, "Failed to send event telemetry for {EventName}", eventName);
            }
        }
    }
}
