using System.Threading.Tasks;
using Dapper;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    internal interface IResourceRepository
    {
        Task<DbResource> GetResource(int resourceId, int terminalId);
    }

    internal class ResourceRepository : BaseRepository, IResourceRepository
    {
        private const string ReadQuery = @"
            SELECT PlaceId = t.PlaceId
                FROM dbo.Resources as r JOIN dbo.Terminals as t
                on r.TerminalId = t.TerminalId
                WHERE r.TerminalId = @TerminalId AND r.ResourceId = @ResourceId";
        
        public ResourceRepository(DatabaseConfiguration database) : base(database)
        {
        }

        public Task<DbResource> GetResource(int resourceId, int terminalId)
        {
            return ExecuteAsync(connection => connection.QuerySingleAsync<DbResource>(ReadQuery,
                new {ResourceId = resourceId, TerminalId = terminalId},
                commandTimeout: CommandTimeout));
        }
    }
}