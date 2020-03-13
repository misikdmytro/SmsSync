using System.Threading;
using System.Threading.Tasks;
using SmsSync.Models;

namespace SmsSync.Templates
{
    internal class TicketIdBuilder : ITemplateBuilder
    {
        public Task<string> Build(DbSms sms, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(sms.OrderId.ToString());
        }
    }
}