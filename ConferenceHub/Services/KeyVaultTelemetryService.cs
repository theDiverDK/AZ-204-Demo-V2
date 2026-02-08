using System.Diagnostics;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ConferenceHub.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;

namespace ConferenceHub.Services
{
    public interface IKeyVaultTelemetryService
    {
        Task ProbeAsync(string operation);
    }

    public class KeyVaultTelemetryService : IKeyVaultTelemetryService
    {
        private readonly KeyVaultTelemetryConfig _config;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<KeyVaultTelemetryService> _logger;

        public KeyVaultTelemetryService(
            IOptions<KeyVaultTelemetryConfig> config,
            TelemetryClient telemetryClient,
            ILogger<KeyVaultTelemetryService> logger)
        {
            _config = config.Value;
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public async Task ProbeAsync(string operation)
        {
            if (string.IsNullOrWhiteSpace(_config.VaultUri) || string.IsNullOrWhiteSpace(_config.ProbeSecretName))
            {
                return;
            }

            var startTime = DateTimeOffset.UtcNow;
            var sw = Stopwatch.StartNew();
            var target = new Uri(_config.VaultUri).Host;
            var dependencyName = $"GetSecret:{_config.ProbeSecretName}";

            try
            {
                var client = new SecretClient(new Uri(_config.VaultUri), new DefaultAzureCredential());
                await client.GetSecretAsync(_config.ProbeSecretName);

                _telemetryClient.TrackDependency("Azure Key Vault", target, dependencyName, operation, startTime, sw.Elapsed, "OK", true);
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackDependency("Azure Key Vault", target, dependencyName, operation, startTime, sw.Elapsed, "ERROR", false);
                _telemetryClient.TrackEvent("KeyVaultProbeFailed", new Dictionary<string, string>
                {
                    ["Operation"] = operation,
                    ["VaultUri"] = _config.VaultUri,
                    ["SecretName"] = _config.ProbeSecretName,
                    ["Error"] = ex.Message
                });

                _logger.LogWarning(ex, "Key Vault probe failed for operation {Operation}", operation);
            }
        }
    }
}
