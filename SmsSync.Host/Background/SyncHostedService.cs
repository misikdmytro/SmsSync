using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Equality;
using SmsSync.Models;
using SmsSync.Services;

namespace SmsSync.Background
{
    internal class SyncHostedService : BackgroundService
    {
        private readonly ILogger _logger = Log.ForContext<SyncHostedService>();

        private readonly IInboxRepository _inboxRepository;
        private readonly IChainSmsHandler _chainSmsHandler;
        private readonly BackgroundConfiguration _backgroundConfiguration;
        private readonly HashSet<DbSms> _smsSet;

        private readonly int _maxBatchSize;
        private readonly List<Task> _tasks;

        public SyncHostedService(IChainSmsHandler chainSmsHandler,
            IInboxRepository inboxRepository, BackgroundConfiguration backgroundConfiguration, 
            int maxBatchSize)
        {
            _chainSmsHandler = chainSmsHandler;
            _inboxRepository = inboxRepository;
            _backgroundConfiguration = backgroundConfiguration;

            if (maxBatchSize <= 0)
            {
	            throw new ArgumentException("Batch size should be positive", nameof(maxBatchSize));
            }
            
            _maxBatchSize = maxBatchSize;

            _smsSet = new HashSet<DbSms>(_maxBatchSize * 2, new SmsEqualityComparer());
            _tasks = new List<Task>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Start main threads.");
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
	            while (!cancellationToken.IsCancellationRequested)
	            {
                    try
                    {
                        var currentBatch = _maxBatchSize - _tasks.Count;

                        var messages = Array.Empty<DbSms>();
                        if (currentBatch > 0)
                        {
                            messages = await _inboxRepository.TakeAndPromote(Constants.States.New,
                                Constants.States.InProgress, currentBatch, cancellationToken);
                        }

                        foreach (var sms in messages)
                        {
                            if (_smsSet.Add(sms))
                            {
                                var task = _chainSmsHandler.HandleAsync(sms, cancellationToken)
                                    .ContinueWith(t =>
                                    {
                                        if (!t.IsCompletedSuccessfully)
                                        {
                                            _logger.Error(t.Exception, "Task completed with errors. Sms {@Sms}", sms);
                                        }

                                        _smsSet.Remove(sms);
                                        _logger.Debug("Sms {@Sms} removed from set", sms);
                                    }, CancellationToken.None);

                                _tasks.Add(task);
                            }
                        }

                        _tasks.RemoveAll(t => t.IsCompleted);
                        await Task.Delay(_backgroundConfiguration.PingInterval, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        _logger.Warning(e, "Unhandled exception thrown. Restart after {@Interval}", _backgroundConfiguration.PingInterval);
                        await Task.Delay(_backgroundConfiguration.PingInterval, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException oce)
            {
	            _logger.Warning(oce, "Operation was canceled. Ignore.");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            await Task.WhenAll(_tasks).ConfigureAwait(false);
            _logger.Information("Stop main threads.");
        }
    }
}