using System.Linq;
using System.Threading.Tasks;
using Dapper;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface IInboxRepository
    {
        Task<DbSms[]> ReadAsync();
        Task Commit(DbSms message);
        Task Fail(DbSms message);
    }

    public class InboxRepository : BaseRepository, IInboxRepository
    {
        private const string NewState = "NEW";
        private const string SentState = "SENT";
        private const string FailState = "FAIL";

        private const string ReadQuery = @"
                    SELECT *
                        FROM dbo.SmsEvents
                        WHERE State IN (@States)";

        private const string UpdateQuery = @"
                    UPDATE dbo.SmsEvents
                        SET State = @State, LastUpdateTime = CURRENT_TIMESTAMP
                        WHERE OrderId = @OrderId AND TerminalId = @TerminalId AND State = @CurrentState
                            AND SetTime = @SetTime AND LastUpdateTime = @LastUpdateTime";

        private readonly string[] _statesToSelect = {NewState};

        public InboxRepository(DatabaseConfiguration database)
            : base(database)
        {
        }

        public Task<DbSms[]> ReadAsync()
        {
            return ExecuteAsync(async connection =>
            {
                var sms = await connection.QueryAsync<DbSms>(ReadQuery,
                    new {States = _statesToSelect},
                    commandTimeout: CommandTimeout);

                return sms.ToArray();
            });
        }

        public Task Commit(DbSms message)
        {
            return ExecuteAsync(connection => connection.ExecuteAsync(UpdateQuery,
                new
                {
                    message.OrderId, message.TerminalId, State = SentState, CurrentState = message.State,
                    message.LastUpdateTime, message.SetTime
                },
                commandTimeout: CommandTimeout));
        }

        public Task Fail(DbSms message)
        {
            return ExecuteAsync(connection => connection.ExecuteAsync(UpdateQuery,
                new
                {
                    message.OrderId, message.TerminalId, State = FailState, CurrentState = message.State,
                    message.LastUpdateTime, message.SetTime
                },
                commandTimeout: CommandTimeout));
        }
    }
}