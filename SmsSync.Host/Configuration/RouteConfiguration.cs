using System.Collections.Generic;

namespace SmsSync.Configuration
{
    public class RouteConfiguration
    {
        public string Name { get; set; }
        public string Route { get; set; }
        public string Method { get; set; }
        public IDictionary<string, string> Body { get; set; }
    }
}