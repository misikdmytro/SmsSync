using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;
using Serilog;
using SmsSync.Configuration;

namespace SmsSync.Services
{
    public interface IMessageHttpService
    {
        Task SendSms(object message, CancellationToken cancellationToken = default);
    }

    public class MessageHttpService : IMessageHttpService
    {
        private const string SendMessageRoute = "sendMessage";
        
        private readonly ILogger _logger = Log.ForContext<MessageHttpService>();

        private readonly IHttpClientsPool _httpClientsPool;
        private readonly IDictionary<string, string> _routes;

        private readonly int _retryCount;
        private readonly TimeSpan _retryInterval;

        public MessageHttpService(HttpConfiguration configuration, IHttpClientsPool httpClientsPool)
        {
            _httpClientsPool = httpClientsPool;
            
            _routes = configuration.Routes;
            _retryCount = configuration.Retry;
            _retryInterval = configuration.RetryInterval;
        }

        public Task SendSms(object message, CancellationToken cancellationToken = default)
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(_retryCount,
                    i => _retryInterval,
                    (exception, ts, i, context) =>
                    {
                        _logger.Warning(exception, "Retry at {N} http call after {@TimeSpan}", i, ts);
                    })
                .ExecuteAsync(async () =>
                {
                    var typeFormatter = new JsonMediaTypeFormatter
                    {
                        SerializerSettings =
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        }
                    };

                    var httpClient = _httpClientsPool.TakeHttpClient();

                    using (var request = new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json))
                    using (var response = await httpClient.PostAsync(_routes[SendMessageRoute], request, cancellationToken))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            throw new InvalidOperationException($"External service returned {response.StatusCode} status code. Content {content}");
                        }
                    }
                });
        }
    }
}