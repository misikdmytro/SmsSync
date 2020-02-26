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
    }

    #region Fake

    public class FakeInboxRepository : IInboxRepository
    {
        public Task<DbSms[]> ReadAsync()
        {
            var guid = Guid.NewGuid();
            return Task.FromResult(new[]
            {
                new DbSms
                {
                    ClientPhone = guid.ToString(),
                    LanguageId = 0,
                    OrderId = guid.GetHashCode()
                }
            });
        }

        public Task Commit(DbSms[] messages)
        {
            // ToDo: update models
            return Task.CompletedTask;
        }
    }

    #endregion

    #region Real

    public class InboxRepository : IInboxRepository
    {
        private const string NewState = "NEW"; 
        private const string SentState = "SENT"; 
        
        private readonly DatabaseConfiguration _database;

        public InboxRepository(DatabaseConfiguration database)
        {
            _database = database;
        }

        public async Task<DbSms[]> ReadAsync()
        {
            using (var connection = CreateConnection())
            {
                const string query = @"""
                    SELECT LanguageId, OrderId, ClientPhone, TerminalId, SetTime, LastUpdateTime, State
                        FROM dbo.SmsEvents
                        WHERE State = @State""";
                
                var sms = await connection.QueryAsync<DbSms>(query,
                    new {State = NewState},
                    commandTimeout:_database.Timeout);

                return sms.ToArray();
            }
        }

        public async Task Commit(DbSms[] messages)
        {
            using (var connection = CreateConnection())
            {
                foreach (var message in messages)
                {
                    const string query = @"""
                    UPDATE dbo.SmsEvents
                        SET State = @State, LastUpdateTime = CURRENT_TIMESTAMP
                        WHERE OrderId = @OrderId AND TerminalId = @TerminalId""";

                    await connection.ExecuteAsync(query, 
                        new {message.OrderId, message.TerminalId, State = SentState},
                        commandTimeout:_database.Timeout);
                }
            }
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_database.ConnectionString);
        }
    }

    #endregion
}