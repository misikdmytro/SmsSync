using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    internal interface IResourceRepository
    {
        Task<DbResource> GetResource(int resourceId, int terminalId, CancellationToken cancellationToken = default);
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

        public Task<DbResource> GetResource(int resourceId, int terminalId, CancellationToken cancellationToken = default)
        {
            var query = BuildQuery(
                () => new { ResourceId = resourceId, TerminalId = terminalId },
                (connection, command) => connection.QuerySingleAsync<DbResource>(command),
                ReadQuery);
            
            return ExecuteAsync(query, cancellationToken);
        }
    }
}