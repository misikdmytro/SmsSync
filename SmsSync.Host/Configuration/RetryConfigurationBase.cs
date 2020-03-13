using System;

namespace SmsSync.Configuration
{
    internal class RetryConfigurationBase
    {
        public int Retry { get; set; }
        public TimeSpan RetryInterval { get; set; }
    }
}