using System;
using System.Data;
using System.Data.SqlClient;
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
        Task Commit(DbSms[] messages);
        Task Fail(DbSms[] messages);
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
                        WHERE OrderId = @OrderId AND TerminalId = @TerminalId AND State = @CurrentState";

        private readonly string[] _statesToSelect = {NewState};

        public InboxRepository(DatabaseConfiguration database)
            :base(database)
        {
        }

        public async Task<DbSms[]> ReadAsync()
        {
            using (var connection = CreateConnection())
            {
                var sms = await connection.QueryAsync<DbSms>(ReadQuery,
                    new {States = _statesToSelect},
                    commandTimeout: Timeout);

                return sms.ToArray();
            }
        }

        public async Task Commit(DbSms[] messages)
        {
            using (var connection = CreateConnection())
            {
                foreach (var message in messages)
                {
                    await connection.ExecuteAsync(UpdateQuery,
                        new {message.OrderId, message.TerminalId, State = SentState, CurrentState = message.State},
                        commandTimeout: Timeout);
                }
            }
        }

        public async Task Fail(DbSms[] messages)
        {
            using (var connection = CreateConnection())
            {
                foreach (var message in messages)
                {
                    await connection.ExecuteAsync(UpdateQuery,
                        new {message.OrderId, message.TerminalId, State = FailState, CurrentState = message.State},
                        commandTimeout: Timeout);
                }
            }
        }
    }
}