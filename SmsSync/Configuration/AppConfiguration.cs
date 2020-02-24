using System;

namespace SmsSync.Configuration
{
    public class AppConfiguration
    {
        public int WorkersCount { get; set; }
        public TimeSpan ReadInterval { get; set; }
        public TimeSpan SyncInterval { get; set; }
    }
}