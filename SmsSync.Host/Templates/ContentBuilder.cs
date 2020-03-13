using System;
using System.Threading;
using System.Threading.Tasks;
using SmsSync.Configuration;
using SmsSync.Models;

namespace SmsSync.Templates
{
    internal class ContentBuilder : ITemplateBuilder
    {
        private readonly ResourcesConfiguration _resourcesConfiguration;

        public ContentBuilder(ResourcesConfiguration resourcesConfiguration)
        {
            _resourcesConfiguration = resourcesConfiguration;
        }

        public Task<string> Build(DbSms sms, CancellationToken cancellationToken = default)
        {
            var messageType = sms.ResourceId == -1
                ? Constants.Resources.Types.Registration
                : Constants.Resources.Types.Invitation;

            if (_resourcesConfiguration.Messages.TryGetValue(messageType, out var resource) &&
                resource.TryGetValue(sms.LanguageId.ToString("D"), out var messageContent))
            {
                return Task.FromResult(messageContent);
            }
            
            throw new ArgumentException($"Message not found. Type: {messageType}. Language: {sms.LanguageId}");
        }
    }
}