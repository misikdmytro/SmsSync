using System;

namespace SmsSync.Configuration
{
    public class DatabaseConfiguration : RetryConfigurationBase
    {
        public string ConnectionString { get; set; }
        public int Timeout { get; set; }
    }
}