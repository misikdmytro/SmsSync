using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SmsSync.Extensions
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseRecipeInfrastructure(this IHostBuilder builder, Func<IWebHostBuilder, IWebHostBuilder> action)
        {
            return builder
                .UseSerilog((context, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration))
                .ConfigureWebHostDefaults(webBuilder => action(webBuilder)
                    .UseContentRoot(Directory.GetCurrentDirectory()));

        }
    }
}