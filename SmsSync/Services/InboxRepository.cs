using System;
using System.Threading.Tasks;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface IInboxRepository
    {
        Task<Notification[]> ReadAsync();
        Task Commit(params Notification[] messages);
    }

    public class FakeInboxRepository : IInboxRepository
    {
        public Task<Notification[]> ReadAsync()
        {
            var guid = Guid.NewGuid();
            return Task.FromResult(new[]
            {
                new Notification(guid.GetHashCode(), guid.ToString())
            });
        }

        public Task Commit(params Notification[] messages)
        {
            // ToDo: update models
            return Task.CompletedTask;
        }
    }
}