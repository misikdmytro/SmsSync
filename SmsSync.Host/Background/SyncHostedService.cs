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

        private readonly IOutboxManager _outboxManager;
        private readonly IMessageService _messageService;
        
        private readonly IList<BaclgroundTimer> _timers;

        public SyncHostedService(BackgroundConfiguration backgroundConfiguration, IMapper mapper, IOutboxManager outboxManager, IMessageService messageService)
        {
            _mapper = mapper;
            _outboxManager = outboxManager;
            _messageService = messageService;
            
            _timers = Enumerable.Range(0, backgroundConfiguration.WorkersCount)
                .Select(x => new BaclgroundTimer(backgroundConfiguration.SyncInterval))
                .ToList();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Start main threads...");

            foreach (var timer in _timers)
            {
                timer.Start(async (sender, args) =>
                {
                    // 1. Take message
                    Notification notification;
                    while ((notification = _outboxManager.Next(OutboxNotification.NotificationState.New)) != null)
                    {
                        try
                        {
                            // 2. Map to HTTP contracts
                            var message = _mapper.Map<Message>(notification);
                        
                            // 3. Send using HTTP
                            await _messageService.SendSms(message, CancellationToken.None);
                            
                            // 4. Mark as sent
                            _outboxManager.Promote(notification);
                        }
                        catch (Exception e)
                        {
                            _outboxManager.Rollback(notification);
                            _logger.Error(e, "Error during send message");
                        }
                    }
                });
            }
            
            _logger.Information("Main threads started");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Stop main threads...");

            foreach (var timer in _timers)
            {
                timer.Stop();
            }
            
            _logger.Information("Main threads stoped");
            
            return Task.CompletedTask;
        }
    }
}