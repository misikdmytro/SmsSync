using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    internal interface IInboxRepository
    {
        Task<DbSms[]> TakeAndPromote(string oldState, string newState, int batchSize = int.MaxValue, 
            CancellationToken  cancellationToken = default);
        Task<DbSms[]> TakeAndPromote(DbSms dbSms, string newState, int batchSize = int.MaxValue, 
            CancellationToken  cancellationToken = default);
    }

    internal class InboxRepository : BaseRepository, IInboxRepository
    {
        private const string UpdateQueryByState = @"
                    UPDATE dboSmsEvent
                        SET State = @State, LastUpdateTime = CURRENT_TIMESTAMP
                            OUTPUT INSERTED.*
                        FROM (SELECT TOP (@BatchSize) * 
		                    FROM dbo.SmsEvents
                            WHERE State = @CurrentState
                            ORDER BY SetTime ASC) AS dboSmsEvent";

        private const string UpdateQueryBySms = @"
                    UPDATE dboSmsEvent
                        SET State = @State, LastUpdateTime = CURRENT_TIMESTAMP
                            OUTPUT INSERTED.*
                        FROM (SELECT TOP (@BatchSize) * 
		                    FROM dbo.SmsEvents
                            WHERE OrderId = @OrderId AND TerminalId = @TerminalId AND State = @CurrentState
                                AND SetTime = @SetTime AND LastUpdateTime = @LastUpdateTime
                            ORDER BY SetTime ASC) AS dboSmsEvent";

        public InboxRepository(DatabaseConfiguration database)
            : base(database)
        {
        }

        public Task<DbSms[]> TakeAndPromote(string oldState, string newState, int batchSize = int.MaxValue, 
            CancellationToken cancellationToken = default)
        {
            var query = BuildQuery(
                () => new { BatchSize = batchSize, State = newState, CurrentState = oldState },
                async (connection, command) =>
                {
                    var sms = await connection.QueryAsync<DbSms>(command);
                    return sms.ToArray();
                },
                UpdateQueryByState);
            
            return ExecuteAsync(query, cancellationToken);
        }

        public Task<DbSms[]> TakeAndPromote(DbSms dbSms, string newState, int batchSize = int.MaxValue, 
            CancellationToken cancellationToken = default)
        {
            var query = BuildQuery(
                () => new
                {
                    dbSms.OrderId,
                    dbSms.TerminalId,
                    State = newState,
                    CurrentState = dbSms.State,
                    dbSms.LastUpdateTime,
                    dbSms.SetTime,
                    BatchSize = batchSize,
                },
                async (connection, command) =>
                {
                    var sms = await connection.QueryAsync<DbSms>(command);
                    return sms.ToArray();
                },
                UpdateQueryBySms);
            
            return ExecuteAsync(query, cancellationToken);
        }
    }
}