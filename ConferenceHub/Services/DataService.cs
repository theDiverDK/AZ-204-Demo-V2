using ConferenceHub.Models;
using System.Text.Json;

namespace ConferenceHub.Services
{
    public interface IDataService
    {
        Task<List<Session>> GetSessionsAsync();
        Task<Session?> GetSessionByIdAsync(int id);
        Task AddSessionAsync(Session session);
        Task UpdateSessionAsync(Session session);
        Task DeleteSessionAsync(int id);
        Task<List<Registration>> GetRegistrationsAsync();
        Task AddRegistrationAsync(Registration registration);
    }

    public class DataService : IDataService
    {
        private readonly string _seedSessionsFilePath;
        private List<Session> _sessions;
        private List<Registration> _registrations;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public DataService(IWebHostEnvironment env)
        {
            _seedSessionsFilePath = Path.Combine(env.ContentRootPath, "Data", "sessions.json");
            _sessions = new List<Session>();
            _registrations = new List<Registration>();
            LoadSessionsAsync().Wait();
        }

        private async Task LoadSessionsAsync()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                if (File.Exists(_seedSessionsFilePath))
                {
                    var jsonContent = await File.ReadAllTextAsync(_seedSessionsFilePath);
                    _sessions = JsonSerializer.Deserialize<List<Session>>(jsonContent, options) ?? new List<Session>();
                }
            }
            catch (Exception)
            {
                _sessions = new List<Session>();
            }
        }

        public async Task<List<Session>> GetSessionsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _sessions.OrderBy(s => s.StartTime).ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Session?> GetSessionByIdAsync(int id)
        {
            await _semaphore.WaitAsync();
            try
            {
                return _sessions.FirstOrDefault(s => s.Id == id);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task AddSessionAsync(Session session)
        {
            await _semaphore.WaitAsync();
            try
            {
                session.Id = _sessions.Any() ? _sessions.Max(s => s.Id) + 1 : 1;
                _sessions.Add(session);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task UpdateSessionAsync(Session session)
        {
            await _semaphore.WaitAsync();
            try
            {
                var existingSession = _sessions.FirstOrDefault(s => s.Id == session.Id);
                if (existingSession != null)
                {
                    _sessions.Remove(existingSession);
                    _sessions.Add(session);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DeleteSessionAsync(int id)
        {
            await _semaphore.WaitAsync();
            try
            {
                var session = _sessions.FirstOrDefault(s => s.Id == id);
                if (session != null)
                {
                    _sessions.Remove(session);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<Registration>> GetRegistrationsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _registrations.ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task AddRegistrationAsync(Registration registration)
        {
            await _semaphore.WaitAsync();
            try
            {
                registration.Id = _registrations.Any() ? _registrations.Max(r => r.Id) + 1 : 1;
                registration.RegisteredAt = DateTime.Now;
                _registrations.Add(registration);

                // Update session registration count
                var session = _sessions.FirstOrDefault(s => s.Id == registration.SessionId);
                if (session != null)
                {
                    session.CurrentRegistrations++;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
