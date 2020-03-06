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
        private readonly IHttpClientsPool _httpClientsPool;

        public MessageSendServiceFactory(HttpConfiguration httpConfiguration, IHttpClientsPool httpClientsPool)
        {
            _httpConfiguration = httpConfiguration;
            _httpClientsPool = httpClientsPool;
        }

        public IMessageHttpService CreateHttpService()
        {
            return new MessageHttpService(_httpConfiguration, _httpClientsPool);
        }
    }
}
