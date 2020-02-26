using System.Threading.Tasks;
using Dapper;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface IResourceRepository
    {
        Task<DbResource> GetResource(int resourceId, int terminalId);
    }

    public class ResourceRepository : BaseRepository, IResourceRepository
    {
        private const string ReadQuery = @"
            SELECT PlaceId = t.PlaceId
                FROM dbo.Resources as r JOIN dbo.Terminals as t
                on r.TerminalId = t.TerminalId
                WHERE r.TerminalId = @TerminalId AND r.ResourceId = @ResourceId";
        
        public ResourceRepository(DatabaseConfiguration database) : base(database)
        {
        }

        public async Task<DbResource> GetResource(int resourceId, int terminalId)
        {
            using (var connection = CreateConnection())
            {
                return await connection.QuerySingleAsync<DbResource>(ReadQuery,
                    new {ResourceId = resourceId, TerminalId = terminalId},
                    commandTimeout: CommandTimeout);
            }
        }
    }
}