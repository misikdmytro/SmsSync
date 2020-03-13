using System.Threading.Tasks;
using SmsSync.Models;

namespace SmsSync.Templates
{
    internal class TicketIdBuilder : ITemplateBuilder
    {
        public Task<string> Build(DbSms sms)
        {
            return Task.FromResult(sms.OrderId.ToString());
        }
    }
}