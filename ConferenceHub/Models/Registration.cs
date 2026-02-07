namespace ConferenceHub.Models
{
    public class Registration
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string AttendeeName { get; set; } = string.Empty;
        public string AttendeeEmail { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }
}
