using System.Data;
using System.Data.SqlClient;
using SmsSync.Configuration;

namespace SmsSync.Services
{
    public class BaseRepository
    {
        private readonly DatabaseConfiguration _database;

        protected int Timeout => _database.Timeout;

        public BaseRepository(DatabaseConfiguration database)
        {
            _database = database;
        }
        
        protected IDbConnection CreateConnection()
        {
            return new SqlConnection(_database.ConnectionString);
        }
    }
}