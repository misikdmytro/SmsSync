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

        public MessageBuilder(ResourcesConfiguration configuration, IJobsRepository jobsRepository)
        {
            _converters =
                new Dictionary<string, Func<DbSms, Task<string>>>
                {
                    [Constants.Resources.TicketIdPlaceholder] = sms => Task.FromResult(sms.OrderId.ToString()),
                    [Constants.Resources.PlaceIdPlaceholder] = sms => Task.FromResult("PLACEID"),
                    [Constants.Resources.ServiceIdPlaceholder] = sms => GetJobDescription(sms.JobId, sms.TerminalId, sms.LanguageId)
                };
            
            _configuration = configuration;
            _jobsRepository = jobsRepository;
        }

        public async Task<Message> Build(DbSms sms)
        {
            if (_configuration.Messages.TryGetValue(sms.JobDescription, out var resource) &&
                resource.TryGetValue(sms.LanguageId.ToString("D"), out var messageContent))
            {
                var content = await BuildContent(sms, messageContent);
                
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

        private async Task<string> GetJobDescription(int jobId, int terminalId, Language languageId)
        {
            var job = await _jobsRepository.GetJobById(jobId, terminalId);

            switch (languageId)
            {
                case Language.Default:
                    // default 
                    return job.DescriptionUa;
                case Language.Russian:
                    // russian
                    return job.DescriptionRu;
                case Language.Ukrainian:
                    // ukrainian
                    return job.DescriptionUa;
                case Language.English:
                    // english
                    return job.DescriptionEn;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown language {languageId}", nameof(languageId));
            }
        }
    }
}