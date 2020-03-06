using System;
using System.Collections.Generic;
using System.Linq;
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
    public class SyncHostedService : BackgroundService
    {
        private readonly ILogger _logger = Log.ForContext<SyncHostedService>();

        private readonly IInboxRepository _inboxRepository;
        private readonly IChainSmsHandler _chainSmsHandler;
        private readonly BackgroundConfiguration _backgroundConfiguration;
        private readonly HashSet<DbSms> _smsSet;

        private readonly int _batchSize;

        public SyncHostedService(IChainSmsHandler chainSmsHandler,
            IInboxRepository inboxRepository, BackgroundConfiguration backgroundConfiguration, 
            int batchSize)
        {
            _chainSmsHandler = chainSmsHandler;
            _inboxRepository = inboxRepository;
            _backgroundConfiguration = backgroundConfiguration;

            if (batchSize <= 0)
            {
	            throw new ArgumentException("Batch size should be positive", nameof(batchSize));
            }
            
            _batchSize = batchSize;

            _smsSet = new HashSet<DbSms>(_batchSize * 2, new SmsEqualityComparer());
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Start main threads.");
            
            var tasks = new List<Task>();

            try
            {
	            while (!cancellationToken.IsCancellationRequested)
	            {
		            var currentBatch = _batchSize - tasks.Count;
		            
		            var messages = Array.Empty<DbSms>();
		            if (currentBatch > 0)
		            {
			            messages = await _inboxRepository.TakeAndPromote(Constants.States.New, 
				            Constants.States.InProgress, currentBatch);
		            }
		            
		            foreach (var sms in messages)
		            {
			            if (_smsSet.Add(sms))
			            {
				            var task = Task.Run(() => _chainSmsHandler.HandleAsync(sms, cancellationToken),
					            cancellationToken).ContinueWith(t =>
				            {
					            if (!t.IsCompletedSuccessfully)
					            {
						            _logger.Error(t.Exception, "Task completed with errors. Sms {@Sms}", sms);
                                }

                                _smsSet.Remove(sms);
                                _logger.Debug("Sms {@Sms} removed from set", sms);
                            }, cancellationToken);

				            tasks.Add(task);
			            }
		            }

		            tasks.RemoveAll(t => t.IsCompleted);
		            await Task.Delay(_backgroundConfiguration.PingInterval, cancellationToken);
	            }
            }
            catch (OperationCanceledException oce)
            {
	            _logger.Warning(oce, "Operation was canceled. Ignore.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unhandled exception thrown");
                throw;
            }
            finally
            {
                await Task.WhenAll(tasks);
            }
            
            _logger.Information("Stop main threads.");
        }
    }
}