using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
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
        
        private readonly IChainSmsHandler _successor;
        private readonly ISmsHandler _current;
        private readonly IChainSmsHandler _failer;

        public ChainSmsHandler(ISmsHandler current, IChainSmsHandler successor, IChainSmsHandler failer)
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
        private readonly IMessageHttpService _messageHttpService;

        public SendSmsHandler(IMessageBuilder messageBuilder, IMessageHttpService messageHttpService)
        {
            _messageBuilder = messageBuilder;
            _messageHttpService = messageHttpService;
        }

        public async Task HandleAsync(DbSms sms, CancellationToken token = default)
        {
            _logger.Debug("Try to build message. Sms {@Sms}", sms);
            var message = await _messageBuilder.Build(sms);
            
            _logger.Debug("Try to send message. Message {@Message}", message);
            await _messageHttpService.SendSms(message, token);
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