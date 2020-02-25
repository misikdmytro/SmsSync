using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Models;
using SmsSync.Services;

namespace SmsSync.Background
{
    public class PopulateHostedService : IHostedService
    {
        private readonly ILogger _logger = Log.ForContext<PopulateHostedService>();
            
        private readonly IOutboxManager _outboxManager;
        private readonly IInboxRepository _repository;
        
        private readonly BaclgroundTimer _populator;
        private readonly BaclgroundTimer _commitor;

        public PopulateHostedService(BackgroundConfiguration backgroundConfiguration,
            IOutboxManager outboxManager, IInboxRepository repository)
        {
            _outboxManager = outboxManager;
            _repository = repository;

            _populator = new BaclgroundTimer(backgroundConfiguration.ReadInterval);
            _commitor = new BaclgroundTimer(backgroundConfiguration.CommitInterval);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Start commitor and populator...");
            
            _commitor.Start(async (sender, args) =>
            {
                // 1. Collect sent notifications
                var notificationsToCommit = _outboxManager.All(OutboxNotification.NotificationState.Sent);

                try
                {
                    // 2. Commit
                    await _repository.Commit(notificationsToCommit);

                    // 3. Promote notifications
                    _outboxManager.Promote(notificationsToCommit);

                    // 3. Clear outbox
                    _outboxManager.Clear(_outboxManager.All(OutboxNotification.NotificationState.Committed));
                }
                catch (Exception e)
                {
                    _outboxManager.Rollback(notificationsToCommit);
                    _logger.Error(e, "Error during send message");
                }
            });
            
            _populator.Start(async (sender, args) =>
            {
                try
                {
                    // 1. Read data
                    var notifications = await _repository.ReadAsync();
                    
                    // 2. Populate data
                    _outboxManager.Populate(notifications);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error during data population");
                }
            });
            
            _logger.Information("Commitor and populator started");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Stop commitor and populator...");

            _populator.Stop();
            _commitor.Stop();
            
            _logger.Information("Commitor and populator stoped");
            
            return Task.CompletedTask;
        }
    }
}