using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface ISmsHandler
    {
        Task HandleAsync(DbSms sms, CancellationToken token = default);
    }

    public interface IChainSmsHandler : ISmsHandler
    {
    }
    
    public class ChainSmsHandler : IChainSmsHandler
    {
        private readonly ILogger _logger = Log.ForContext<ChainSmsHandler>(); 
        
        private readonly ChainSmsHandler _successor;
        private readonly ISmsHandler _current;
        private readonly ChainSmsHandler _failer;

        public ChainSmsHandler(ISmsHandler current, ChainSmsHandler successor, ChainSmsHandler failer)
        {
            _successor = successor;
            _current = current;
            _failer = failer;
        }

        public async Task HandleAsync(DbSms sms, CancellationToken token = default)
        {
            try
            {
                await _current.HandleAsync(sms, token);
                if (_successor != null)
                {
                    await _successor.HandleAsync(sms, token);
                }
            }
            catch (Exception e)
            {
                if (_failer == null)
                {
                    _logger.Error(e, "Handler failed with exception. No fallback scenario.");
                    throw;
                }
                
                _logger.Warning(e, "Handler failed with exception. Fallback scenario start.");
                await _failer.HandleAsync(sms, token);
            }
        }
    }
    
    public class SendSmsHandler : ISmsHandler
    {
        private readonly ILogger _logger = Log.ForContext<SendSmsHandler>(); 

        private readonly IMessageBuilder _messageBuilder;
        private readonly HttpConfiguration _httpConfiguration;

        public SendSmsHandler(IMessageBuilder messageBuilder, HttpConfiguration httpConfiguration)
        {
            _messageBuilder = messageBuilder;
            _httpConfiguration = httpConfiguration;
        }

        public async Task HandleAsync(DbSms sms, CancellationToken token = default)
        {
            _logger.Debug("Try to build message. Sms {@Sms}", sms);
            var message = await _messageBuilder.Build(sms);
            using (var messageService = new MessageHttpService(_httpConfiguration))
            {
                _logger.Debug("Try to send message. Message {@Message}", message);
                await messageService.SendSms(message, token);
            }
        }
    }
    
    public class CommitSmsHandler : ISmsHandler
    {
        private readonly IInboxRepository _repository;

        public CommitSmsHandler(IInboxRepository repository) => _repository = repository;

        public Task HandleAsync(DbSms sms, CancellationToken token = default) => _repository.TakeAndPromote(sms, Constants.States.Sent);
    }
    
    public class FailSmsHandler : ISmsHandler
    {
        private readonly IInboxRepository _repository;

        public FailSmsHandler(IInboxRepository repository) => _repository = repository;

        public Task HandleAsync(DbSms sms, CancellationToken token = default) => _repository.TakeAndPromote(sms, Constants.States.Fail);
    }
}