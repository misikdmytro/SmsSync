using System;

namespace SmsSync.Configuration
{
    public class RetryConfigurationBase
    {
        public int Retry { get; set; }
        public TimeSpan RetryInterval { get; set; }
    }
}