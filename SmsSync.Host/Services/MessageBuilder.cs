using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using SmsSync.Models;
using SmsSync.Templates;

namespace SmsSync.Services
{
    internal interface IMessageBuilder
    {
        Task<object> Build(DbSms sms, IDictionary<string, string> template);
    }

    internal class MessageBuilder : IMessageBuilder
    {
        private readonly ILogger _logger = Log.ForContext<MessageBuilder>();

        private readonly IDictionary<string, ITemplateBuilder> _templateBuilders;

        public MessageBuilder(IDictionary<string, ITemplateBuilder> templateBuilders)
        {
            _templateBuilders = templateBuilders;
        }

        public async Task<object> Build(DbSms sms, IDictionary<string, string> templateObject)
        {
            // 1. Create empty body
            var body = new JObject();

            foreach (var (key, template) in templateObject)
            {
                // 2. Iterate via each body property
                var value = template;

                do
                {
                    // 3. Replace placeholders with real values
                    foreach (var (templateKey, templateBuilder) in _templateBuilders.Where(tb => value.Contains(tb.Key)))
                    {
                        value = value.Replace(templateKey, await templateBuilder.Build(sms));
                    }
                } while (_templateBuilders.Any(tb => value.Contains(tb.Key)));

                // 4. Set real value to message
                body[key] = value;
            }

            _logger.Information("Build message {Message}", body.ToString(Formatting.None));

            return body;
        }
    }
}