using System.Threading.Tasks;
using SmsSync.Models;

namespace SmsSync.Templates
{
    public class PhoneNumberBuilder : ITemplateBuilder
    {
        public Task<string> Build(DbSms sms)
        {
            return Task.FromResult(sms.ClientPhone);
        }
    }
}