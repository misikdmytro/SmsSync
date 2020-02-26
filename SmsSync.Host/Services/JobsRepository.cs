using System.Threading.Tasks;
using Dapper;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface IJobsRepository
    {
        Task<DbJob> GetJobById(int jobId, int terminalId);
    }

    public class JobsRepository : BaseRepository, IJobsRepository
    {
        private const string GetJobQuery = @"
            SELECT DescriptionRu = Description_ru, DescriptionUa = Description_ua, DescriptionEn = Description_en
                FROM dbo.Jobs
                WHERE JobId = @JobId AND TerminalId = @TerminalId";
        
        public JobsRepository(DatabaseConfiguration database) : base(database)
        {
        }
        
        public async Task<DbJob> GetJobById(int jobId, int terminalId)
        {
            using (var connection = CreateConnection())
            {
                return await connection.QuerySingleAsync<DbJob>(GetJobQuery,
                    new {JobId = jobId, TerminalId = terminalId},
                    commandTimeout: CommandTimeout);
            }
        }
    }
}