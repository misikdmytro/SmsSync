using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmsSync.Background;
using SmsSync.Configuration;
using SmsSync.Mapper;
using SmsSync.Services;

namespace SmsSync
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        
        public Startup(IHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            
            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = Configuration.GetSection("ServiceConfig").Get<AppConfiguration>();

            services.AddSingleton(configuration);
            services.AddSingleton(configuration.Background);
            services.AddSingleton(configuration.Http);
            
            services.AddTransient<InboxRepository>();
            services.AddTransient<MessageService>();

            services.AddSingleton<InboxManager>();
            
            var mapper = new MapperConfiguration(config => config.AddProfile<MessageProfile>())
                .CreateMapper();

            services.AddSingleton(mapper);
            
            services.AddHostedService<PopulateHostedService>();
            services.AddHostedService<SyncHostedService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
        }
    }
}