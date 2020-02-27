using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
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
        private readonly BaclgroundTimer _failer;

        public PopulateHostedService(BackgroundConfiguration backgroundConfiguration,
            IOutboxManager outboxManager, IInboxRepository repository)
        {
            _outboxManager = outboxManager;
            _repository = repository;

            _populator = new BaclgroundTimer(backgroundConfiguration.ReadInterval);
            _commitor = new BaclgroundTimer(backgroundConfiguration.CommitInterval);
            _failer = new BaclgroundTimer(backgroundConfiguration.FailInterval);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Start DB timers...");
            
            _commitor.Start(async (sender, args) =>
            {
                // 1. Collect sent notifications
                var notificationsToCommit = _outboxManager.All(OutboxNotification.NotificationState.Sent);

                try
                {
                    // 2. Commit
                    await _repository.Commit(notificationsToCommit.Select(x => x.Sms).ToArray());

                    // 3. Promote notifications
                    _outboxManager.Promote(notificationsToCommit);

                    // 3. Clear outbox
                    _outboxManager.Clear(_outboxManager.All(OutboxNotification.NotificationState.Committed));
                }
                catch (Exception e)
                {
                    _outboxManager.Rollback(notificationsToCommit);
                    _logger.Error(e, "Error during commit message");
                }
            });
            
            _populator.Start(async (sender, args) =>
            {
                try
                {
                    // 1. Read data
                    var notifications = await _repository.ReadAsync();
                    
                    // 2. Populate data
                    _outboxManager.Populate(notifications.Select(x => new Notification(x)).ToArray());
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error during data population");
                }
            });
            
            _failer.Start(async (sender, args) =>
            {
                // 1. Collect failed notifications
                var notificationsToCommit = _outboxManager.All(OutboxNotification.NotificationState.Failed);

                try
                {
                    // 2. Commit
                    await _repository.Fail(notificationsToCommit.Select(x => x.Sms).ToArray());

                    // 3. Promote notifications
                    _outboxManager.Promote(notificationsToCommit);

                    // 3. Clear outbox
                    _outboxManager.Clear(_outboxManager.All(OutboxNotification.NotificationState.Marked));
                }
                catch (Exception e)
                {
                    _outboxManager.Rollback(notificationsToCommit);
                    _logger.Error(e, "Error during failing message");
                }
            });
            
            _logger.Information("DB timers started");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Stop DB timers...");

            _populator.Stop();
            _commitor.Stop();
            _failer.Stop();
            
            _logger.Information("DB timers stopped");
            
            return Task.CompletedTask;
        }
    }
}