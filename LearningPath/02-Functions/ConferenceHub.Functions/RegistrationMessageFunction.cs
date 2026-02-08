using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ConferenceHub.Functions;

public sealed class RegistrationMessageFunction
{
    private readonly ILogger<RegistrationMessageFunction> _logger;

    public RegistrationMessageFunction(ILogger<RegistrationMessageFunction> logger)
    {
        _logger = logger;
    }

    [Function("ProcessRegistrationMessage")]
    public void Run(
        [ServiceBusTrigger("%ServiceBusTopicName%", "%ServiceBusSubscriptionName%", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
    {
        var traceParent = GetTraceParent(message);
        using var activity = StartConsumerActivity("ProcessRegistrationMessage", traceParent);

        _logger.LogInformation("SERVICEBUS_MESSAGE_RECEIVED");
        var payload = JsonSerializer.Deserialize<RegistrationPayload>(
            message.Body.ToString(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new RegistrationPayload();

        var sender = Environment.GetEnvironmentVariable("CONFIRMATION_SENDER_EMAIL") ?? "noreply@conferencehub.local";
        var receiver = payload.AttendeeEmail ?? string.Empty;
        var subject = $"ConferenceHub Confirmation: {payload.SessionTitle}";
        var body =
            $"Hello {payload.AttendeeName}, you are registered for '{payload.SessionTitle}' in room {payload.Room} starting {payload.SessionStartTime:yyyy-MM-dd HH:mm}.";

        _logger.LogInformation("Confirmation email log output from Service Bus message");
        _logger.LogInformation("Sender: {Sender}", sender);
        _logger.LogInformation("Receiver: {Receiver}", receiver);
        _logger.LogInformation("Subject: {Subject}", subject);
        _logger.LogInformation("Body: {Body}", body);
    }

    private static string? GetTraceParent(ServiceBusReceivedMessage message)
    {
        if (message.ApplicationProperties.TryGetValue("traceparent", out var traceParentObj))
        {
            return traceParentObj?.ToString();
        }

        if (message.ApplicationProperties.TryGetValue("Diagnostic-Id", out var diagnosticIdObj))
        {
            return diagnosticIdObj?.ToString();
        }

        return null;
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

    private sealed class RegistrationPayload
    {
        public int SessionId { get; set; }
        public string? SessionTitle { get; set; }
        public string? AttendeeName { get; set; }
        public string? AttendeeEmail { get; set; }
        public DateTime SessionStartTime { get; set; }
        public string? Room { get; set; }
    }
}
