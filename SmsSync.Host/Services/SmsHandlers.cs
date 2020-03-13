using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    internal interface ISmsHandler
    {
        Task HandleAsync(DbSms sms, CancellationToken token = default);
    }

    internal interface IChainSmsHandler : ISmsHandler
    {
    }
    
    internal class ChainSmsHandler : IChainSmsHandler
    {
        private readonly ILogger _logger = Log.ForContext<ChainSmsHandler>(); 
        
        private readonly IChainSmsHandler _successor;
        private readonly ISmsHandler _current;
        private readonly IChainSmsHandler _fallback;

        public ChainSmsHandler(ISmsHandler current, IChainSmsHandler successor, IChainSmsHandler fallback)
        {
            _successor = successor;
            _current = current;
            _fallback = fallback;
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
                if (_fallback == null)
                {
                    _logger.Error(e, "Handler failed with exception. No fallback scenario.");
                    throw;
                }
                
                _logger.Warning(e, "Handler failed with exception. Fallback scenario start.");
                await _fallback.HandleAsync(sms, token);
            }
        }
    }
    
    internal class SendSmsHandler : ISmsHandler
    {
        private readonly ILogger _logger = Log.ForContext<SendSmsHandler>();

        private readonly IMessageBuilder _messageBuilder;
        private readonly IMessageHttpService _messageHttpService;
        private readonly RouteConfiguration _routeConfiguration;

        public SendSmsHandler(IMessageBuilder messageBuilder, IMessageHttpService messageHttpService, 
            RouteConfiguration routeConfiguration)
        {
            _messageBuilder = messageBuilder;
            _messageHttpService = messageHttpService;
            _routeConfiguration = routeConfiguration;
        }

        public async Task HandleAsync(DbSms sms, CancellationToken token = default)
        {
            _logger.Debug("Try to build message. Sms {@Sms}", sms);
            var message = await _messageBuilder.Build(sms, _routeConfiguration.Body, token);
            
            _logger.Debug("Try to send message. Message {@Message}", message);
            await _messageHttpService.SendSms(_routeConfiguration, message, token);
        }
    }
    
    internal class PromoteSmsHandler : ISmsHandler
    {
        private readonly IInboxRepository _repository;
        private readonly string _promoteState;

        protected PromoteSmsHandler(IInboxRepository repository, string promoteState)
        {
            _repository = repository;
            _promoteState = promoteState;
        }

        // operation should be done even if cancellation requested
        public Task HandleAsync(DbSms sms, CancellationToken token = default) => _repository.TakeAndPromote(sms, _promoteState, cancellationToken: CancellationToken.None);
    }
    
    internal class CommitSmsHandler : PromoteSmsHandler
    {
        public CommitSmsHandler(IInboxRepository repository) : base(repository, Constants.States.Sent)
        {
        }
    }
    
    internal class FailSmsHandler : PromoteSmsHandler
    {
        public FailSmsHandler(IInboxRepository repository) : base(repository, Constants.States.Fail)
        {
        }
    }
}