using System;
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
        void Rollback(UserMessage message);
        Task CommitAll();
    }

    public class InboxManager : IInboxManager
    {
        private class MessageWrapper
        {
            private enum MessageState
            {
                New,
                Waiting,
                Sent
            }

            public UserMessage Message { get; }
            public bool CanBeSend => _state == MessageState.New;
            public bool AlreadySend => _state == MessageState.Waiting;

            private MessageState _state;

            public MessageWrapper(UserMessage message)
            {
                Message = message;
                _state = MessageState.New;
            }

            public void Promote()
            {
                _state = _state switch
                {
                    MessageState.New => MessageState.Waiting,
                    MessageState.Waiting => MessageState.Sent,
                    MessageState.Sent => MessageState.Sent,
                    _ => throw new ArgumentOutOfRangeException(nameof(_state))
                };
            }

            public void Rollback()
            {
                _state = _state switch
                {
                    MessageState.New => MessageState.New,
                    MessageState.Waiting => MessageState.New,
                    MessageState.Sent => MessageState.Sent,
                    _ => throw new ArgumentOutOfRangeException(nameof(_state))
                };
            }
        }

        private readonly ILogger _logger = Log.ForContext<InboxManager>();

        private readonly ConcurrentDictionary<long, MessageWrapper> _messages;

        private readonly IInboxRepository _repository;

        private readonly object _lock = new object();

        public InboxManager(IInboxRepository repository)
        {
            _repository = repository;
            _messages = new ConcurrentDictionary<long, MessageWrapper>();
        }

        public async Task Populate()
        {
            var data = await _repository.ReadAsync();

            _logger.Information("Populate queue with {N} messages", data.Length);
            lock (_lock)
            {
                foreach (var message in data)
                {
                    _messages.TryAdd(message.TicketNumber, new MessageWrapper(message));
                }
            }
        }

        public bool TakeToSend(out UserMessage message)
        {
            lock (_lock)
            {
                var (_, value) = _messages.FirstOrDefault(m => m.Value.CanBeSend);
                value?.Promote();
                message = value?.Message;
                return message != null;
            }
        }

        public void MarkAsSend(UserMessage message)
        {
            lock (_lock)
            {
                _messages[message.TicketNumber].Promote();
            }
        }

        public void Rollback(UserMessage message)
        {
            lock (_lock)
            {
                _messages[message.TicketNumber].Rollback();
            }
        }

        public async Task CommitAll()
        {
            UserMessage[] messagesToCommit;
            lock (_lock)
            {
                messagesToCommit = _messages.Where(m => m.Value.AlreadySend)
                    .Select(x => x.Value.Message)
                    .ToArray();
            }

            if (!messagesToCommit.Any())
            {
                return;
            }

            await _repository.Commit(messagesToCommit);

            lock (_lock)
            {
                foreach (var message in messagesToCommit)
                {
                    _messages.Remove(message.TicketNumber, out _);
                }
            }
        }
    }
}