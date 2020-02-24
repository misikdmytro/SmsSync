using AutoMapper;
using SmsSync.Models;

namespace SmsSync.Mapper
{
    public class MessageProfile : Profile
    {
        public MessageProfile()
        {
            CreateMap<UserMessage, Message>()
                .BeforeMap((src, dest) =>
                {
                    dest.Source = Constants.Source;
                    dest.BearerType = Constants.BearerType;
                    dest.ServiceType = Constants.ServiceType;
                    dest.ContentType = Constants.ContentType;
                })
                .ForMember(dest => dest.Destination, src => src.MapFrom(x => x.PhoneNumber))
                // ToDo: localization
                .ForMember(dest => dest.Content, src => src.Ignore());
        }
    }
}