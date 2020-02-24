using System;
using System.Collections.Concurrent;
using System.Linq;
using Serilog;

namespace SmsSync.Services
{
    public class Queue<TKey, TValue>
    {
        private readonly ILogger _logger = Log.ForContext<Queue<TKey, TValue>>();

        private readonly ConcurrentDictionary<TKey, TValue> _models;

        public Queue()
        {
            _models = new ConcurrentDictionary<TKey, TValue>();
        }

        public void Add(TKey key, TValue value)
        {
            _logger.Debug("Model {@Model} with key {@Key} added", value, key);
            _models[key] = value;
        }

        public void Add<TModel>(TModel model, Func<TModel, (TKey key, TValue value)> map)
        {
            var (key, value) = map(model);
            Add(key, value);
        }

        public TValue Take(TKey key)
        {
            if (_models.TryRemove(key, out var model))
            {
                _logger.Debug("Model {@Model} taken", model);
            }

            return model;
        }

        public (TKey, TValue) Take()
        {
            while (_models.Any())
            {
                var model = _models.FirstOrDefault();
                if (!model.Equals(default))
                {
                    var result = Take(model.Key);
                    if (!result.Equals(default))
                    {
                        return (model.Key, result);
                    }
                }
            }

            return (default, default);
        }
    }
}