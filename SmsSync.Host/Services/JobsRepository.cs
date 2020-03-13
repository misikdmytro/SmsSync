using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    internal interface IJobsRepository
    {
        Task<DbJob> GetJobById(int jobId, int terminalId, CancellationToken cancellationToken = default);
    }

    internal class JobsRepository : BaseRepository, IJobsRepository
    {
        private const string GetJobQuery = @"
            SELECT DescriptionRu = Description_ru, DescriptionUa = Description_ua, DescriptionEn = Description_en
                FROM dbo.Jobs
                WHERE JobId = @JobId AND TerminalId = @TerminalId";
        
        public JobsRepository(DatabaseConfiguration database) : base(database)
        {
        }
        
        public Task<DbJob> GetJobById(int jobId, int terminalId, CancellationToken cancellationToken = default)
        {
            var query = BuildQuery(
                () => new { JobId = jobId, TerminalId = terminalId },
                (connection, command) => connection.QuerySingleAsync<DbJob>(command),
                GetJobQuery);
            
            return ExecuteAsync(query, cancellationToken);
        }
    }
}