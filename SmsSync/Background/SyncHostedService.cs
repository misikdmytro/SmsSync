using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Hosting;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Models;
using SmsSync.Services;

namespace SmsSync.Background
{
    public class SyncHostedService : IHostedService
    {
        private readonly ILogger _logger = Log.ForContext<SyncHostedService>();
        
        private readonly IMapper _mapper;

        private readonly IInboxManager _inboxManager;
        private readonly IMessageService _messageService;
        
        private readonly IList<BaclgroundTimer> _timers;

        public SyncHostedService(BackgroundConfiguration backgroundConfiguration, IMapper mapper, IInboxManager inboxManager, IMessageService messageService)
        {
            _mapper = mapper;
            _inboxManager = inboxManager;
            _messageService = messageService;
            
            _timers = Enumerable.Range(0, backgroundConfiguration.WorkersCount)
                .Select(x => new BaclgroundTimer(backgroundConfiguration.SyncInterval))
                .ToList();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var timer in _timers)
            {
                timer.Start(async (sender, args) =>
                {
                    // 1. Take message
                    while (_inboxManager.TakeToSend(out var userMessage))
                    {
                        try
                        {
                            _logger.Information("Take message {@Message}", userMessage);
                            
                            // 2. Map to HTTP contracts
                            var message = _mapper.Map<Message>(userMessage);
                        
                            // 3. Send using HTTP
                            await _messageService.SendSms(message, CancellationToken.None);
                            
                            // 4. Mark as sent
                            _inboxManager.MarkAsSend(userMessage);
                        }
                        catch (Exception e)
                        {
                            if (userMessage != null)
                            {
                                _inboxManager.Rollback(userMessage);
                            }
                            
                            _logger.Error(e, "Error during send message");
                        }
                    }
                });
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var timer in _timers)
            {
                timer.Stop();
                timer.Dispose();
            }
            
            return Task.CompletedTask;
        }
    }
}