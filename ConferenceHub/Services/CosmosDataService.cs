using ConferenceHub.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ConferenceHub.Services
{
    public class CosmosDataService : IDataService
    {
        private readonly Container _sessionsContainer;
        private readonly Container _registrationsContainer;
        private readonly TelemetryClient _telemetryClient;

        public CosmosDataService(IOptions<CosmosDbConfig> config, TelemetryClient telemetryClient)
        {
            var cfg = config.Value;

            if (string.IsNullOrWhiteSpace(cfg.Endpoint) || string.IsNullOrWhiteSpace(cfg.Key))
            {
                throw new InvalidOperationException("CosmosDb configuration is missing Endpoint or Key.");
            }

            var client = new CosmosClient(cfg.Endpoint, cfg.Key);
            var database = client.GetDatabase(cfg.DatabaseName);
            _sessionsContainer = database.GetContainer(cfg.SessionsContainerName);
            _registrationsContainer = database.GetContainer(cfg.RegistrationsContainerName);
            _telemetryClient = telemetryClient;
        }

        public async Task<List<Session>> GetSessionsAsync()
        {
            var sessionDocs = await QueryAllAsync<SessionDocument>(_sessionsContainer, "SELECT * FROM c");
            var registrationDocs = await QueryAllAsync<RegistrationDocument>(_registrationsContainer, "SELECT * FROM c");

            var countBySession = registrationDocs
                .GroupBy(r => r.SessionId)
                .ToDictionary(g => g.Key, g => g.Count());

            return sessionDocs
                .Select(d =>
                {
                    var session = ToSession(d);
                    session.CurrentRegistrations = countBySession.GetValueOrDefault(session.Id, 0);
                    return session;
                })
                .OrderBy(s => s.StartTime)
                .ToList();
        }

        public async Task<Session?> GetSessionByIdAsync(int id)
        {
            try
            {
                var response = await _sessionsContainer.ReadItemAsync<SessionDocument>(
                    id.ToString(),
                    new PartitionKey(id.ToString()));

                var session = ToSession(response.Resource);
                session.CurrentRegistrations = await GetRegistrationCountAsync(id);
                return session;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task AddSessionAsync(Session session)
        {
            session.Id = await GetNextIdAsync(_sessionsContainer);
            session.CurrentRegistrations = 0;

            var document = ToSessionDocument(session);
            await _sessionsContainer.CreateItemAsync(document, new PartitionKey(document.id));
        }

        public async Task UpdateSessionAsync(Session session)
        {
            session.CurrentRegistrations = await GetRegistrationCountAsync(session.Id);
            var document = ToSessionDocument(session);
            await _sessionsContainer.UpsertItemAsync(document, new PartitionKey(document.id));
        }

        public async Task DeleteSessionAsync(int id)
        {
            try
            {
                await _sessionsContainer.DeleteItemAsync<SessionDocument>(id.ToString(), new PartitionKey(id.ToString()));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }

            var query = new QueryDefinition("SELECT * FROM c WHERE c.SessionId = @sessionId")
                .WithParameter("@sessionId", id);

            var registrations = await QueryAllAsync<RegistrationDocument>(_registrationsContainer, query);
            foreach (var registration in registrations)
            {
                await _registrationsContainer.DeleteItemAsync<RegistrationDocument>(
                    registration.id,
                    new PartitionKey(registration.partitionKey));
            }
        }

        public async Task<List<Registration>> GetRegistrationsAsync()
        {
            var registrationDocs = await QueryAllAsync<RegistrationDocument>(_registrationsContainer, "SELECT * FROM c");
            return registrationDocs
                .OrderByDescending(r => r.RegisteredAt)
                .Select(ToRegistration)
                .ToList();
        }

        public async Task AddRegistrationAsync(Registration registration)
        {
            registration.Id = await GetNextIdAsync(_registrationsContainer);
            registration.RegisteredAt = DateTime.UtcNow;

            var document = ToRegistrationDocument(registration);
            await _registrationsContainer.CreateItemAsync(document, new PartitionKey(document.partitionKey));
        }

        private async Task<int> GetRegistrationCountAsync(int sessionId)
        {
            var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.SessionId = @sessionId")
                .WithParameter("@sessionId", sessionId);

            var counts = await QueryAllAsync<int>(_registrationsContainer, query);
            return counts.FirstOrDefault();
        }

        private async Task<List<T>> QueryAllAsync<T>(Container container, string queryText)
        {
            return await QueryAllAsync<T>(container, new QueryDefinition(queryText));
        }

        private async Task<List<T>> QueryAllAsync<T>(Container container, QueryDefinition query)
        {
            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var iterator = container.GetItemQueryIterator<T>(query);
                var results = new List<T>();
                while (iterator.HasMoreResults)
                {
                    var page = await iterator.ReadNextAsync();
                    results.AddRange(page);
                }

                TrackCosmosQuery(container.Id, query.QueryText, startTime, stopwatch.Elapsed, true, string.Empty);
                return results;
            }
            catch (Exception ex)
            {
                TrackCosmosQuery(container.Id, query.QueryText, startTime, stopwatch.Elapsed, false, ex.Message);
                throw;
            }
        }

        private async Task<int> GetNextIdAsync(Container container)
        {
            var values = await QueryAllAsync<int?>(container, "SELECT VALUE MAX(c.Id) FROM c");
            var max = values.FirstOrDefault();
            return max.GetValueOrDefault() + 1;
        }

        private void TrackCosmosQuery(string containerName, string queryText, DateTimeOffset startTime, TimeSpan duration, bool success, string errorMessage)
        {
            _telemetryClient.TrackDependency(
                dependencyTypeName: "Azure Cosmos DB",
                target: containerName,
                dependencyName: "Query",
                data: queryText,
                startTime: startTime,
                duration: duration,
                resultCode: success ? "OK" : "ERROR",
                success: success);

            _telemetryClient.TrackEvent("CosmosDbQueryExecuted", new Dictionary<string, string>
            {
                ["Container"] = containerName,
                ["Query"] = queryText,
                ["Success"] = success.ToString(),
                ["Error"] = errorMessage
            });
        }

        private static Session ToSession(SessionDocument doc)
        {
            return new Session
            {
                Id = doc.Id,
                Title = doc.Title,
                Speaker = doc.Speaker,
                StartTime = doc.StartTime,
                EndTime = doc.EndTime,
                Room = doc.Room,
                Description = doc.Description,
                Capacity = doc.Capacity,
                CurrentRegistrations = doc.CurrentRegistrations,
                SlideUrls = doc.SlideUrls ?? new List<string>()
            };
        }

        private static SessionDocument ToSessionDocument(Session session)
        {
            return new SessionDocument
            {
                id = session.Id.ToString(),
                Id = session.Id,
                Title = session.Title,
                Speaker = session.Speaker,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Room = session.Room,
                Description = session.Description,
                Capacity = session.Capacity,
                CurrentRegistrations = session.CurrentRegistrations,
                SlideUrls = session.SlideUrls ?? new List<string>()
            };
        }

        private static Registration ToRegistration(RegistrationDocument doc)
        {
            return new Registration
            {
                Id = doc.Id,
                SessionId = doc.SessionId,
                AttendeeName = doc.AttendeeName,
                AttendeeEmail = doc.AttendeeEmail,
                RegisteredAt = doc.RegisteredAt
            };
        }

        private static RegistrationDocument ToRegistrationDocument(Registration registration)
        {
            return new RegistrationDocument
            {
                id = registration.Id.ToString(),
                partitionKey = registration.SessionId.ToString(),
                Id = registration.Id,
                SessionId = registration.SessionId,
                AttendeeName = registration.AttendeeName,
                AttendeeEmail = registration.AttendeeEmail,
                RegisteredAt = registration.RegisteredAt
            };
        }

        private sealed class SessionDocument
        {
            public string id { get; set; } = string.Empty;
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Speaker { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string Room { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int Capacity { get; set; }
            public int CurrentRegistrations { get; set; }
            public List<string> SlideUrls { get; set; } = new();
        }

        private sealed class RegistrationDocument
        {
            public string id { get; set; } = string.Empty;
            public string partitionKey { get; set; } = string.Empty;
            public int Id { get; set; }
            public int SessionId { get; set; }
            public string AttendeeName { get; set; } = string.Empty;
            public string AttendeeEmail { get; set; } = string.Empty;
            public DateTime RegisteredAt { get; set; }
        }
    }
}
