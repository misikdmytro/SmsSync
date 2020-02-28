using System.Linq;
using System.Threading.Tasks;
using Dapper;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface IInboxRepository
    {
        Task<DbSms[]> TakeAndPromote(string oldState, string newState);
        Task<DbSms[]> TakeAndPromote(DbSms dbSms, string newState);
    }

    public class InboxRepository : BaseRepository, IInboxRepository
    {
        private const string UpdateQueryByState = @"
                    UPDATE dbo.SmsEvents
                        SET State = @State, LastUpdateTime = CURRENT_TIMESTAMP
                            OUTPUT INSERTED.*
                        WHERE State = @CurrentState";

        private const string UpdateQueryBySms = @"
                    UPDATE dbo.SmsEvents
                        SET State = @State, LastUpdateTime = CURRENT_TIMESTAMP
                            OUTPUT INSERTED.*
                        WHERE OrderId = @OrderId AND TerminalId = @TerminalId AND State = @CurrentState
                            AND SetTime = @SetTime AND LastUpdateTime = @LastUpdateTime";

        public InboxRepository(DatabaseConfiguration database)
            : base(database)
        {
        }

        public Task<DbSms[]> TakeAndPromote(string oldState, string newState)
        {
            return ExecuteAsync(async connection =>
            {
                var sms = await connection.QueryAsync<DbSms>(UpdateQueryByState,
                    new { State = newState, CurrentState = oldState },
                    commandTimeout: CommandTimeout);

                return sms.ToArray();
            });
        }

        public Task<DbSms[]> TakeAndPromote(DbSms dbSms, string newState)
        {
            return ExecuteAsync(async connection =>
            {
                var sms = await connection.QueryAsync<DbSms>(UpdateQueryBySms,
                    new
                    {
                        dbSms.OrderId,
                        dbSms.TerminalId,
                        State = newState,
                        CurrentState = dbSms.State,
                        dbSms.LastUpdateTime,
                        dbSms.SetTime
                    },
                    commandTimeout: CommandTimeout);

                return sms.ToArray();
            });
        }
    }
}