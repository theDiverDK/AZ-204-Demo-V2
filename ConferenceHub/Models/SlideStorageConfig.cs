namespace ConferenceHub.Models
{
    public class SlideStorageConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = "session-slides";
    }
}
