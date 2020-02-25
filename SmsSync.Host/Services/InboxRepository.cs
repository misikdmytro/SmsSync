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
    
    public class InboxRepository : IInboxRepository
    {
        private DatabaseConfiguration _database;

        public InboxRepository(DatabaseConfiguration database)
        {
            _database = database;
        }

        public async Task<DbSms[]> ReadAsync()
        {
            using (var connection = CreateConnection())
            {
                const string query = @"""
                    SELECT LanguageId, OrderId, ClientPhone, TerminalId
                        FROM dbo.SmsEvents
                        WHERE State = 'NEW'""";
                
                var sms = await connection.QueryAsync<DbSms>(query,
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
                        SET State = 'SENT'
                        WHERE OrderId = @OrderId AND TerminalId = @TerminalId""";

                    await connection.ExecuteAsync(query, new {message.OrderId, message.TerminalId},
                        commandTimeout:_database.Timeout);
                }
            }
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_database.ConnectionString);
        }
    }
}