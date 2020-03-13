using System;

namespace SmsSync.Configuration
{
    internal class HttpConfiguration : RetryConfigurationBase
    {
        public TimeSpan Timeout { get; set; }
        public int PoolSize { get; set; }
    }
}