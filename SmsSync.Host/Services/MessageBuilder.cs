using System;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface IMessageBuilder
    {
        Message Build(DbSms sms);
    }

    public class MessageBuilder : IMessageBuilder
    {
        private readonly ILogger _logger = Log.ForContext<MessageBuilder>();
        
        private readonly ResourcesConfiguration _configuration;

        public MessageBuilder(ResourcesConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Message Build(DbSms sms)
        {
            if (_configuration.Messages.TryGetValue(Constants.Resources.ResourceName, out var resource) &&
                resource.TryGetValue(sms.LanguageId.ToString(), out var messageContent))
            {
                var content = messageContent
                    .Replace(Constants.Resources.OrderIdPlaceholder, sms.OrderId.ToString())
                    //ToDo: replace with real
                    .Replace(Constants.Resources.ServiceIdPlaceholder, "DON'T KNOW");
                
                var message = new Message
                {
                    Content = content,
                    Destination = sms.ClientPhone,
                    Source = Constants.MessageData.Source,
                    BearerType = Constants.MessageData.BearerType,
                    ContentType = Constants.MessageData.ContentType,
                    ServiceType = Constants.MessageData.ServiceType
                };
                
                _logger.Information("Build message {@Message}", message);


                return message;
            }

            throw new ArgumentOutOfRangeException($"Unknown localization {sms.LanguageId}", nameof(sms.LanguageId));
        }
    }
}