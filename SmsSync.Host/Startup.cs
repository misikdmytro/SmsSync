using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmsSync.Background;
using SmsSync.Configuration;
using SmsSync.Mapper;
using SmsSync.Services;

namespace SmsSync
{
    public class Startup
    {
        private IHostingEnvironment _environment;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            
            Configuration = builder.Build();
            _environment = env;
        }

        public IConfigurationRoot Configuration { get;  }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            
            var configuration = Configuration.GetSection("ServiceConfig").Get<AppConfiguration>();

            services.AddSingleton(configuration);
            services.AddSingleton(configuration.Background);
            services.AddSingleton(configuration.Http);
            services.AddSingleton(configuration.Database);

            if (_environment.IsDevelopment())
            {
                services.AddTransient<IMessageService, FakeMessageService>();
                services.AddTransient<IInboxRepository, FakeInboxRepository>();
            }
            else
            {
                services.AddTransient<IMessageService, MessageService>();
                services.AddTransient<IInboxRepository>(sp =>
                    new InboxRepository(configuration.Database));
            }
            
            services.AddSingleton<IOutboxManager, OutboxManager>();
            
            var mapper = new MapperConfiguration(config => config.AddProfile<MessageProfile>())
                .CreateMapper();

            services.AddSingleton(mapper);
            
            services.AddHostedService<PopulateHostedService>();
            services.AddHostedService<SyncHostedService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}