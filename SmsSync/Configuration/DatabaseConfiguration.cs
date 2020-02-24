using System;

namespace SmsSync.Configuration
{
    public class DatabaseConfiguration
    {
        public string ConnectionString { get; set; }
        public TimeSpan Timeout { get; set; }
    }
}