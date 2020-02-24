using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface IMessageService : IDisposable
    {
        Task SendSms(Message message, CancellationToken cancellationToken);
    }

    public class MessageService : IMessageService
    {
        private readonly ILogger _logger = Log.ForContext<MessageService>();

        private readonly HttpClient _httpClient;
        private readonly int _retryCount;

        public MessageService(HttpConfiguration configuration)
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
        }

        public Task SendSms(Message message, CancellationToken cancellationToken)
        {
            return Policy
                .Handle<InvalidOperationException>()
                .RetryAsync(_retryCount, (exception, i) => _logger.Warning(exception, "Retry http call"))
                .ExecuteAsync(async () =>
                {
                    var response = await _httpClient.PostAsync($"api/contents",
                        new ObjectContent<Message>(message, new JsonMediaTypeFormatter(), 
                            System.Net.Mime.MediaTypeNames.Application.Json),
                        cancellationToken);

                    if (!response.IsSuccessStatusCode)
                        throw new InvalidOperationException(
                            $"External service returned {response.StatusCode} status code");
                });
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}