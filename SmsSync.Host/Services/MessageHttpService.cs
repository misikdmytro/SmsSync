using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Polly;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface IMessageHttpService : IDisposable
    {
        Task SendSms(Message message, CancellationToken cancellationToken = default);
    }

    public class MessageHttpService : IMessageHttpService
    {
        private readonly ILogger _logger = Log.ForContext<MessageHttpService>();

        private readonly HttpClient _httpClient;
        private readonly int _retryCount;
        private readonly TimeSpan _retryInterval;

        public MessageHttpService(HttpConfiguration configuration)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(configuration.BaseUrl),
                Timeout = configuration.Timeout,
                DefaultRequestHeaders =
                {
                    Authorization = new AuthenticationHeaderValue(configuration.TokenScheme, configuration.TokenValue)
                }
            };

            _retryCount = configuration.Retry;
            _retryInterval = configuration.RetryInterval;
        }

        public Task SendSms(Message message, CancellationToken cancellationToken = default)
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
                    
                    var response = await _httpClient.PostAsync("api/contents",
                        new ObjectContent<Message>(message, typeFormatter,
                            System.Net.Mime.MediaTypeNames.Application.Json),
                        cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        throw new InvalidOperationException(
                            $"External service returned {response.StatusCode} status code. Content {content}");
                    }
                });
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}