using System.Text.Json;
using Azure.Messaging.ServiceBus;
using ConferenceHub.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ConferenceHub.Services
{
    public interface IRegistrationMessagePublisher
    {
        Task PublishAsync(RegistrationMessage message);
    }

    public class RegistrationMessagePublisher : IRegistrationMessagePublisher
    {
        private readonly ServiceBusConfig _config;
        private readonly TelemetryClient _telemetryClient;
        private readonly IKeyVaultTelemetryService _keyVaultTelemetryService;
        private readonly ILogger<RegistrationMessagePublisher> _logger;

        public RegistrationMessagePublisher(
            IOptions<ServiceBusConfig> config,
            TelemetryClient telemetryClient,
            IKeyVaultTelemetryService keyVaultTelemetryService,
            ILogger<RegistrationMessagePublisher> logger)
        {
            _config = config.Value;
            _telemetryClient = telemetryClient;
            _keyVaultTelemetryService = keyVaultTelemetryService;
            _logger = logger;
        }

        public async Task PublishAsync(RegistrationMessage message)
        {
            if (string.IsNullOrWhiteSpace(_config.ConnectionString) || string.IsNullOrWhiteSpace(_config.TopicName))
            {
                _logger.LogInformation("Service Bus messaging is not configured. Skipping registration event.");
                return;
            }

            try
            {
                await _keyVaultTelemetryService.ProbeAsync("ServiceBusPublish");

                await using var client = new ServiceBusClient(_config.ConnectionString);
                ServiceBusSender sender = client.CreateSender(_config.TopicName);
                var body = JsonSerializer.Serialize(message);
                var outgoingMessage = new ServiceBusMessage(body);
                var traceParent = Activity.Current?.Id;
                if (!string.IsNullOrWhiteSpace(traceParent))
                {
                    outgoingMessage.ApplicationProperties["traceparent"] = traceParent;
                }
                var startTime = DateTimeOffset.UtcNow;
                var sw = Stopwatch.StartNew();
                await sender.SendMessageAsync(outgoingMessage);
                _telemetryClient.TrackDependency("Azure Service Bus", _config.TopicName, "Publish", "RegistrationCreated", startTime, sw.Elapsed, "OK", true);
                _telemetryClient.TrackEvent("ServiceBusMessagePublished", new Dictionary<string, string>
                {
                    ["Topic"] = _config.TopicName,
                    ["SessionId"] = message.SessionId.ToString()
                });
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackEvent("ServiceBusMessagePublishFailed", new Dictionary<string, string>
                {
                    ["Topic"] = _config.TopicName,
                    ["SessionId"] = message.SessionId.ToString(),
                    ["Error"] = ex.Message
                });
                _logger.LogError(ex, "Failed to publish registration message for session {SessionId}", message.SessionId);
            }
        }
    }

    public class RegistrationMessage
    {
        public int SessionId { get; set; }
        public string SessionTitle { get; set; } = string.Empty;
        public string AttendeeName { get; set; } = string.Empty;
        public string AttendeeEmail { get; set; } = string.Empty;
        public DateTime SessionStartTime { get; set; }
        public string Room { get; set; } = string.Empty;
    }
}
