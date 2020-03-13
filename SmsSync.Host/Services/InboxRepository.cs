using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    internal interface IInboxRepository
    {
        Task<DbSms[]> TakeAndPromote(string oldState, string newState, int batchSize = int.MaxValue);
        Task<DbSms[]> TakeAndPromote(DbSms dbSms, string newState, int batchSize = int.MaxValue);
    }

    internal class InboxRepository : BaseRepository, IInboxRepository
    {
        private readonly ILogger _logger = Log.ForContext<InboxRepository>();

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

        public Task<DbSms[]> TakeAndPromote(string oldState, string newState, int batchSize = int.MaxValue)
        {
            return ExecuteAsync(async connection =>
            {
                var @params = new { BatchSize = batchSize, State = newState, CurrentState = oldState };

                _logger.Debug("Execute {MethodName} with parameters {@Params}", nameof(TakeAndPromote), @params);

                var sms = await connection.QueryAsync<DbSms>(UpdateQueryByState,
                    @params,
                    commandTimeout: CommandTimeout);

                return sms.ToArray();
            });
        }

        public Task<DbSms[]> TakeAndPromote(DbSms dbSms, string newState, int batchSize = int.MaxValue)
        {
            return ExecuteAsync(async connection =>
            {
                var @params = new
                {
                    dbSms.OrderId,
                    dbSms.TerminalId,
                    State = newState,
                    CurrentState = dbSms.State,
                    dbSms.LastUpdateTime,
                    dbSms.SetTime,
                    BatchSize = batchSize,
                };

                _logger.Debug("Execute {MethodName} with parameters {@Params}", nameof(TakeAndPromote), @params);

                var sms = await connection.QueryAsync<DbSms>(UpdateQueryBySms,
                    @params,
                    commandTimeout: CommandTimeout);

                return sms.ToArray();
            });
        }
    }
}