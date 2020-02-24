using System;

namespace SmsSync.Configuration
{
    public class HttpConfiguration
    {
        public string BaseUrl { get; set; }
        public TimeSpan Timeout { get; set; }
        public int Retry { get; set; }
    }
}