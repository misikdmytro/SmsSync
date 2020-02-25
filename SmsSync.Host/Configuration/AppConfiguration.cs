namespace SmsSync.Configuration
{
    public class AppConfiguration
    {
        public BackgroundConfiguration Background { get; set; }
        public HttpConfiguration Http { get; set; }
        public DatabaseConfiguration Database { get; set; }
    }
}