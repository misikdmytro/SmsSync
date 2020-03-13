using System;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Polly;
using Serilog;
using SmsSync.Configuration;

namespace SmsSync.Services
{
    internal class BaseRepository
    {
        private readonly ILogger _logger = Log.ForContext<BaseRepository>();
        
        private readonly DatabaseConfiguration _database;

        protected BaseRepository(DatabaseConfiguration database)
        {
            _database = database;
        }

        protected async Task<T> ExecuteAsync<T>(Func<IDbConnection, CancellationToken, Task<T>> func, CancellationToken cancellationToken = default)
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
                    .ExecuteAsync(token => func(connection, token), cancellationToken);
            }
        }

        protected Func<IDbConnection, CancellationToken, Task<T>> BuildQuery<T>(Func<object> paramsBuilder, 
            Func<IDbConnection, CommandDefinition, Task<T>> func,
            string query,
            [CallerMemberName] string memberName = null)
        {
            return (connection, cancellationToken) =>
            {
                var @params = paramsBuilder();

                _logger.Debug("Execute {MethodName} with parameters {@Params}", memberName, @params);

                var command = new CommandDefinition(query,
                    @params,
                    commandTimeout: _database.Timeout,
                    cancellationToken: cancellationToken);

                return func(connection, command);
            };
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_database.ConnectionString);
        }
    }
}