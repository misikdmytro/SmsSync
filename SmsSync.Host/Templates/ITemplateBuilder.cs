using System.Threading.Tasks;
using SmsSync.Models;

namespace SmsSync.Templates
{
    internal interface ITemplateBuilder
    {
        Task<string> Build(DbSms sms);
    }
}