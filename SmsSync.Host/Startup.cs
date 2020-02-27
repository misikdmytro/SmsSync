using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SmsSync.Background;
using SmsSync.Configuration;
using SmsSync.Services;

namespace SmsSync
{
    public class Startup
    {
        private ILogger _logger = Log.ForContext<Startup>();

        private readonly IHostingEnvironment _environment;

        public Startup(IHostingEnvironment env)
        {
            _logger.Information("Starting service. Environment {Env}", env.EnvironmentName);

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            _environment = env;
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            _logger.Information("Configuring services...");

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            var configuration = Configuration.GetSection("ServiceConfig").Get<AppConfiguration>();

            services.AddSingleton(configuration);
            services.AddSingleton(configuration.Http);
            services.AddSingleton(configuration.Database);
            services.AddSingleton(configuration.Resources);
            
            services.AddTransient<SendSmsHandler>();
            services.AddTransient<CommitSmsHandler>();
            services.AddTransient<FailSmsHandler>();

            services.AddTransient<IChainSmsHandler>(sp =>
            {
                var commitChain = new ChainSmsHandler(sp.GetRequiredService<CommitSmsHandler>(), null, null);
                var failChain = new ChainSmsHandler(sp.GetRequiredService<FailSmsHandler>(), null, null);
                var sendChain = new ChainSmsHandler(sp.GetRequiredService<SendSmsHandler>(), commitChain, failChain);

                return sendChain;
            });
            
            services.AddTransient<IMessageHttpService, MessageHttpService>();
            
            services.AddTransient<IInboxRepository, InboxRepository>();
            services.AddTransient<IJobsRepository, JobsRepository>();
            services.AddTransient<IResourceRepository, ResourceRepository>();

            services.AddSingleton<IMessageBuilder, MessageBuilder>();

            services.AddHostedService<SyncHostedService>();

            _logger.Information("Services configured.");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            _logger.Information("Configuring app...");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            _logger.Information("App configured.");
        }
    }
}