using System.Threading.Tasks;
using SmsSync.Models;

namespace SmsSync.Templates
{
    public interface ITemplateBuilder
    {
        Task<string> Build(DbSms sms);
    }
}