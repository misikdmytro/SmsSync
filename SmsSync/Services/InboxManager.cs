using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            foreach (var message in data)
            {
                _messagesQueue.Enqueue(message);
            }
        }

        public bool TakeToSend(out UserMessage message)
        {
            return _messagesQueue.TryDequeue(out message);
        }

        public void MarkAsSend(UserMessage message)
        {
            _messagesSent.Enqueue(message);
        }

        public async Task CommitAll()
        {
            var messages = new List<UserMessage>();
            while (_messagesSent.TryDequeue(out var message))
            {
                messages.Add(message);
            }

            if (messages.Any())
            {
                await _repository.Commit(messages.ToArray());
            }
        }
    }
}