using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;
using SmsSync.Models;
using SmsSync.Templates;

namespace SmsSync.Services
{
    public interface IMessageBuilder
    {
        Task<JObject> Build(DbSms sms);
    }

    public class MessageBuilder : IMessageBuilder
    {
        private readonly ILogger _logger = Log.ForContext<MessageBuilder>();

        private readonly IDictionary<string, ITemplateBuilder> _templateBuilders;
        private readonly IDictionary<string, string> _template;

        public MessageBuilder(IDictionary<string, ITemplateBuilder> templateBuilders, 
            IDictionary<string, string> template)
        {
            _templateBuilders = templateBuilders;
            _template = template;
        }

        public async Task<JObject> Build(DbSms sms)
        {
            // 1. Create empty body
            var body = new JObject();

            foreach (var (key, template) in _template)
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

            _logger.Information("Build message {@Message}", body);

            return body;
        }
    }
}