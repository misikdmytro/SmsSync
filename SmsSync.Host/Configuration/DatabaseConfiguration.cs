namespace SmsSync.Configuration
{
    internal class DatabaseConfiguration : RetryConfigurationBase
    {
        public string ConnectionString { get; set; }
        public int Timeout { get; set; }
        public int MaxBatchSize { get; set; }
    }
}