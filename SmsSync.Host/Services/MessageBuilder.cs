using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface IMessageBuilder
    {
        Task<Message> Build(DbSms sms);
    }

    public class MessageBuilder : IMessageBuilder
    {
        private readonly ILogger _logger = Log.ForContext<MessageBuilder>();

        private readonly IDictionary<string, Func<DbSms, Task<string>>> _converters;
        
        private readonly ResourcesConfiguration _configuration;
        private readonly IJobsRepository _jobsRepository;
        private readonly IResourceRepository _resourceRepository;

        public MessageBuilder(ResourcesConfiguration configuration, IJobsRepository jobsRepository, IResourceRepository resourceRepository)
        {
            _converters =
                new Dictionary<string, Func<DbSms, Task<string>>>
                {
                    [Constants.Resources.TicketIdPlaceholder] = sms => Task.FromResult(sms.OrderId.ToString()),
                    [Constants.Resources.PlaceIdPlaceholder] = sms => GetPlaceId(sms.ResourceId, sms.TerminalId),
                    [Constants.Resources.ServiceIdPlaceholder] = sms => GetJobDescription(sms.JobId, sms.TerminalId, sms.LanguageId)
                };
            
            _configuration = configuration;
            _jobsRepository = jobsRepository;
            _resourceRepository = resourceRepository;
        }

        public async Task<Message> Build(DbSms sms)
        {
            // 1. Whether 'Registration' or 'Invitation'
            var messageType = GetMessageType(sms);
            
            // 2. Get message format
            if (_configuration.Messages.TryGetValue(messageType, out var resource) &&
                resource.TryGetValue(sms.LanguageId.ToString("D"), out var messageContent))
            {
                // 3. Replace placeholders with real values
                var content = await BuildContent(sms, messageContent);
                
                var message = new Message
                {
                    Content = content,
                    Destination = BuildDestination(sms.ClientPhone),
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

        private async Task<string> BuildContent(DbSms sms, string content)
        {
            foreach (var pattern in _converters)
            {
                if (content.Contains(pattern.Key))
                {
                    var value = await pattern.Value(sms);
                    content = content.Replace(pattern.Key, value);
                }
            }

            return content;
        }

        private string GetMessageType(DbSms sms)
        {
            return sms.ResourceId == -1
                ? Constants.Resources.Types.Registration
                : Constants.Resources.Types.Invitation;
        }
        
        private async Task<string> GetJobDescription(int jobId, int terminalId, Language languageId)
        {
            var job = await _jobsRepository.GetJobById(jobId, terminalId);

            switch (languageId)
            {
                case Language.Default:
                    return job.DescriptionUa;
                case Language.Russian:
                    return job.DescriptionRu;
                case Language.Ukrainian:
                    return job.DescriptionUa;
                case Language.English:
                    return job.DescriptionEn;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown language {languageId}", nameof(languageId));
            }
        }

        private async Task<string> GetPlaceId(int resourceId, int terminalId)
        {
            var resource = await _resourceRepository.GetResource(resourceId, terminalId);
            return resource.PlaceId.ToString();
        }

        private string BuildDestination(string phoneNumber)
        {
            // 1. Remove from number first '+' or '0'
            // +380501234567 -> 380501234567 
            // 0501234567 -> 501234567
            var result = phoneNumber.TrimStart('+')
                .TrimStart('0');

            // 2. Prepend '380' at start if number start with another symbols
            // 501234567 -> 380501234567
            if (!result.StartsWith("380"))
            {
                result = $"380{result}";
            }

            return result;
        }
    }
}