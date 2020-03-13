using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Serilog;
using SmsSync.Configuration;

namespace SmsSync.Services
{
    public interface IHttpClientsPool : IDisposable
    {
        HttpClient TakeHttpClient();
    }

    internal class HttpClientsPool : IHttpClientsPool
    {
        private readonly ILogger _logger = Log.ForContext<HttpClientsPool>();
        
        private volatile int _current = -1;
        private readonly HttpClient[] _pool;
        
        private readonly object _lock = new object();

        public HttpClientsPool(HttpConfiguration configuration)
        {
            if (configuration.PoolSize <= 0)
            {
                throw new ArgumentException("Pool size should be positive", nameof(configuration.PoolSize));
            }
            
            _logger.Debug("Create http pool with {N} clients", configuration.PoolSize);
            
            _pool = Enumerable.Range(0, configuration.PoolSize)
                .Select(x => new HttpClient
                {
                    Timeout = configuration.Timeout
                })
                .ToArray();
        }

        public HttpClient TakeHttpClient()
        {
            lock (_lock)
            {
                if (_current >= _pool.Length - 1)
                {
                    _current = -1;
                }

                return _pool[++_current];
            }
        }

        public void Dispose()
        {
            _logger.Debug("Disposing http pool");
            foreach (var httpClient in _pool)
            {
                httpClient.Dispose();
            }
        }
    }
}