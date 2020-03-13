using System.Threading.Tasks;
using SmsSync.Models;

namespace SmsSync.Templates
{
    internal class PhoneNumberBuilder : ITemplateBuilder
    {
        public Task<string> Build(DbSms sms)
        {
            var phoneNumber = sms.ClientPhone;
            
            // 1. Remove from number first '+' or '0'
            // +380501234567 -> 380501234567 
            // 0501234567 -> 501234567
            var result = phoneNumber.TrimStart('+').TrimStart('0');

            // 2. Prepend '380' at start if number start with another symbols
            // 501234567 -> 380501234567
            if (!result.StartsWith("380"))
            {
                result = $"380{result}";
            }
            
            return Task.FromResult(result);
        }
    }
}