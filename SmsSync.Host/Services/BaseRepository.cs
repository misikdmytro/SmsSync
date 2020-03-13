using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Polly;
using Serilog;
using SmsSync.Configuration;

namespace SmsSync.Services
{
    internal class BaseRepository
    {
        private readonly ILogger _logger = Log.ForContext<BaseRepository>();
        
        private readonly DatabaseConfiguration _database;

        protected int CommandTimeout => _database.Timeout;

        protected BaseRepository(DatabaseConfiguration database)
        {
            _database = database;
        }

        protected async Task<T> ExecuteAsync<T>(Func<IDbConnection, Task<T>> func)
        {
            using (var connection = CreateConnection())
            {
                return await Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(_database.Retry,
                        i => _database.RetryInterval,
                        (exception, ts, i, context) =>
                        {
                            _logger.Warning(exception, "Retry at {N} db query after {@TimeSpan}", i, ts);
                        })
                    .ExecuteAsync(() => func(connection));
            }
        }
        
        protected IDbConnection CreateConnection()
        {
            return new SqlConnection(_database.ConnectionString);
        }
    }
}