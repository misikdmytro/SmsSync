using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface IInboxRepository
    {
        Task<UserMessage[]> ReadAsync();
        Task Commit(params UserMessage[] messages);
    }

    public class InboxRepository : IInboxRepository
    {
        private readonly DatabaseConfiguration _configuration;

        public InboxRepository(DatabaseConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<UserMessage[]> ReadAsync()
        {
            // ToDo: read from SQL
            return new[]
            {
                new UserMessage
                {
                    PhoneNumber = "+1234567",
                    TicketNumber = 4
                },
                new UserMessage
                {
                    PhoneNumber = "+76543210",
                    TicketNumber = 7
                }
            };
        }

        public async Task Commit(params UserMessage[] messages)
        {
            // ToDo: update models
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_configuration.ConnectionString);
        }
    }
}