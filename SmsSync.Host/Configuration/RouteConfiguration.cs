using System.Collections.Generic;

namespace SmsSync.Configuration
{
    internal class RouteConfiguration
    {
        public string Name { get; set; }
        public string Route { get; set; }
        public string Method { get; set; }
        public AuthorizationConfiguration Authorization { get; set; }
        public IDictionary<string, string> Body { get; set; }
    }
}