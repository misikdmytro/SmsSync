using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polly;
using Serilog;
using SmsSync.Configuration;

namespace SmsSync.Services
{
    internal interface IMessageHttpService
    {
        Task SendSms(RouteConfiguration route, object message, CancellationToken cancellationToken = default);
    }

    internal class MessageHttpService : IMessageHttpService
    {
        private readonly ILogger _logger = Log.ForContext<MessageHttpService>();

        private readonly IHttpClientsPool _httpClientsPool;

        private readonly int _retryCount;
        private readonly TimeSpan _retryInterval;

        public MessageHttpService(HttpConfiguration configuration, IHttpClientsPool httpClientsPool)
        {
            _httpClientsPool = httpClientsPool;

            _retryCount = configuration.Retry;
            _retryInterval = configuration.RetryInterval;
        }

        public Task SendSms(RouteConfiguration route, object message, CancellationToken cancellationToken = default)
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(_retryCount,
                    i => _retryInterval,
                    (exception, ts, i, context) =>
                    {
                        _logger.Warning(exception, "Retry at {N} http call after {@TimeSpan}", i, ts);
                    })
                .ExecuteAsync(async token =>
                {
                    var httpClient = _httpClientsPool.TakeHttpClient();

                    using (var content = new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8,
                        MediaTypeNames.Application.Json))
                    using (var request = new HttpRequestMessage(new HttpMethod(route.Method), route.Route)
                        { Content = content })
                    {
                        if (route.Authorization != null)
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue(
                                route.Authorization.TokenScheme,
                                route.Authorization.TokenValue);
                        }

                        using (var response = await httpClient.SendAsync(request, token))
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                var result = await response.Content.ReadAsStringAsync();
                                throw new InvalidOperationException(
                                    $"External service returned {response.StatusCode} status code. Content {result}");
                            }
                        }
                    }
                }, cancellationToken);
        }
    }
}