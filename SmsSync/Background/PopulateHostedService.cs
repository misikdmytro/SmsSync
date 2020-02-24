using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Services;

namespace SmsSync.Background
{
    public class PopulateHostedService : IHostedService
    {
        private readonly ILogger _logger = Log.ForContext<PopulateHostedService>();
            
        private readonly IInboxManager _inboxManager;
        
        private readonly BaclgroundTimer _populator;

        public PopulateHostedService(BackgroundConfiguration backgroundConfiguration,
            IInboxManager inboxManager)
        {
            _inboxManager = inboxManager;

            _populator = new BaclgroundTimer(backgroundConfiguration.ReadInterval);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _populator.Start(async (sender, args) =>
            {
                try
                {
                    // 1. Commit 
                    _logger.Debug("Commit data...");
                    await _inboxManager.CommitAll();

                    // 2. Read data
                    _logger.Debug("Read data...");
                    await _inboxManager.Populate();
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error during data population");
                }
            });
            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _populator.Stop();
            return Task.CompletedTask;
        }
    }
}