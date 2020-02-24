namespace SmsSync.Models
{
    public class Message
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public string ServiceType { get; set; }
        public string BearerType { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }
    }
}