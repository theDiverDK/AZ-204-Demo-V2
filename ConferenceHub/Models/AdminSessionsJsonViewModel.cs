namespace ConferenceHub.Models
{
    public class AdminSessionsJsonViewModel
    {
        public string FilePath { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? Error { get; set; }
        public DateTimeOffset? LastModifiedUtc { get; set; }
        public long? SizeBytes { get; set; }
    }
}
