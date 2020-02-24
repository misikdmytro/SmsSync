using System;
using Microsoft.Extensions.Hosting;

namespace SmsSync.Configuration
{
    public class AppConfiguration
    {
        public BackgroundService Background { get; set; }
        public HttpConfiguration Http { get; set; }
    }
}