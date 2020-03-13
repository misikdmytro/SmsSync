using System;
using System.Threading.Tasks;
using SmsSync.Models;
using SmsSync.Services;

namespace SmsSync.Templates
{
    internal class ServiceIdBuilder : ITemplateBuilder
    {
        private readonly IJobsRepository _jobsRepository;

        public ServiceIdBuilder(IJobsRepository jobsRepository)
        {
            _jobsRepository = jobsRepository;
        }


        public async Task<string> Build(DbSms sms)
        {
            var job = await _jobsRepository.GetJobById(sms.JobId, sms.TerminalId);

            switch (sms.LanguageId)
            {
                case Language.Default:
                    return job.DescriptionUa;
                case Language.Russian:
                    return job.DescriptionRu;
                case Language.Ukrainian:
                    return job.DescriptionUa;
                case Language.English:
                    return job.DescriptionEn;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown language {sms.LanguageId}", nameof(sms.LanguageId));
            }
        }
    }
}