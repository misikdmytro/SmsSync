using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        
        private readonly IOutboxManager _outboxManager;
        private readonly IMessageBuilder _messageBuilder;
        private readonly HttpConfiguration _httpConfiguration;
        
        private readonly IList<BaclgroundTimer> _timers;

        public SyncHostedService(BackgroundConfiguration backgroundConfiguration, HttpConfiguration httpConfiguration, 
            IOutboxManager outboxManager, 
            IMessageBuilder messageBuilder)
        {
            _outboxManager = outboxManager;
            _httpConfiguration = httpConfiguration;
            _messageBuilder = messageBuilder;

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
                    using (var messageService = new MessageHttpService(_httpConfiguration))
                    {
                        // 1. Take message
                        Notification notification;
                        while ((notification = _outboxManager.Next(OutboxNotification.NotificationState.New)) != null)
                        {
                            try
                            {
                                // 2. Build message
                                var message = await _messageBuilder.Build(notification.Sms);

                                // 3. Send using HTTP
                                await messageService.SendSms(message, CancellationToken.None);

                                // 4. Mark as sent
                                _outboxManager.Promote(notification);
                            }
                            catch (Exception e)
                            {
                                _outboxManager.Rollback(notification);
                                _logger.Error(e, "Error during send message");
                            }
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
            
            _logger.Information("Main threads stopped");
            
            return Task.CompletedTask;
        }
    }
}