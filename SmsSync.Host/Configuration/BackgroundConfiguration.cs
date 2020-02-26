using System;

namespace SmsSync.Configuration
{
    public class BackgroundConfiguration
    {
        public int WorkersCount { get; set; }
        public TimeSpan ReadInterval { get; set; }
        public TimeSpan CommitInterval { get; set; }
        public TimeSpan SyncInterval { get; set; }
        public TimeSpan FailInterval { get; set; }
    }
}