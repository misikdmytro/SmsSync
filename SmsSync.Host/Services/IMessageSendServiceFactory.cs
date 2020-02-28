using SmsSync.Configuration;

namespace SmsSync.Services
{
    public interface IMessageSendServiceFactory
    {
        IMessageHttpService CreateHttpService();
    }

    public class MessageSendServiceFactory : IMessageSendServiceFactory
    {
        private readonly HttpConfiguration _httpConfiguration;

        public MessageSendServiceFactory(HttpConfiguration httpConfiguration)
        {
            _httpConfiguration = httpConfiguration;
        }

        public IMessageHttpService CreateHttpService()
        {
            return new MessageHttpService(_httpConfiguration);
        }
    }
}
