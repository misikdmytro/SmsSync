using System.Threading.Tasks;
using SmsSync.Models;
using SmsSync.Services;

namespace SmsSync.Templates
{
    internal class PlaceIdBuilder : ITemplateBuilder
    {
        private readonly IResourceRepository _resourceRepository;

        public PlaceIdBuilder(IResourceRepository resourceRepository)
        {
            _resourceRepository = resourceRepository;
        }

        public async Task<string> Build(DbSms sms)
        {
            var resource = await _resourceRepository.GetResource(sms.ResourceId, sms.TerminalId);
            return resource.PlaceId.ToString();
        }
    }
}