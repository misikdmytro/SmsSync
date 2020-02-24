using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface IInboxManager
    {
        Task Populate();
        bool TakeToSend(out UserMessage message);
        void MarkAsSend(UserMessage message);
        Task CommitAll();
    }

    public class InboxManager : IInboxManager
    {
        private readonly ILogger _logger = Log.ForContext<InboxManager>();

        private readonly ConcurrentQueue<UserMessage> _messagesQueue;
        private readonly ConcurrentQueue<UserMessage> _messagesSent;

        private readonly IInboxRepository _repository;

        public InboxManager(IInboxRepository repository)
        {
            _repository = repository;

            _messagesQueue = new ConcurrentQueue<UserMessage>();
            _messagesSent = new ConcurrentQueue<UserMessage>();
        }

        public async Task Populate()
        {
            var data = await _repository.ReadAsync();
            
            _logger.Information("Populate queue with {N} messages", data.Length);
            
            _messagesQueue.Clear();
            foreach (var message in data)
            {
                _messagesQueue.Enqueue(message);
            }
        }

        public bool TakeToSend(out UserMessage message)
        {
            var result = _messagesQueue.TryDequeue(out message);
            _logger.Debug("Take message {@Message} to send", message);
            return result;
        }

        public void MarkAsSend(UserMessage message)
        {
            _logger.Debug("Mark message {@Message} as send", message);
            _messagesSent.Enqueue(message);
        }

        public async Task CommitAll()
        {
            var messages = _messagesSent.ToList();

            if (messages.Any())
            {
                _logger.Information("Commit {N} messages", messages.Count);
                await _repository.Commit(messages.ToArray());
            }

            _messagesSent.Clear();
        }
    }
}