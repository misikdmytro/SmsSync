using System;
using System.Collections.Concurrent;
using Serilog;

namespace SmsSync.Services
{
    public class MessageQueue<T>
    {
        private readonly ILogger _logger = Log.ForContext<MessageQueue<T>>();

        private readonly BlockingCollection<T> _models;

        public MessageQueue(int boundedCapacity)
        {
            _models = new BlockingCollection<T>(boundedCapacity * 2);
        }

        public void Add(T model)
        {
            _logger.Debug("Model {@Model} added", model);
            _models.Add(model);
        }

        public T Take()
        {
            try
            {
                var model = _models.Take();
                _logger.Debug("Model {@Model} taken", model);
                return model;
            }
            catch (InvalidOperationException ioe)
            {
                _logger.Warning("All data were read from queue");
                return default;
            }
        }
    }
}