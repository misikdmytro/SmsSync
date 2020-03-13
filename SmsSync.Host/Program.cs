using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SmsSync
{
    internal class Program
    {
        private static ILogger Logger => Log.ForContext<Program>();

        public static async Task<int> Main()
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource();

                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    Logger.Information("Cancel requested by user.");
                    cancellationTokenSource.Cancel();
                };

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .CreateLogger();

                var serviceCollection = new ServiceCollection();

                var startup = new Startup(configuration);
                startup.ConfigureServices(serviceCollection);

                using (var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }))
                using (var hostingService = serviceProvider.GetRequiredService<BackgroundService>())
                {
                    var cancellationToken = cancellationTokenSource.Token;
                    
                    try
                    {
                        await hostingService.StartAsync(cancellationToken);
                        while (!cancellationToken.IsCancellationRequested)
                        {
                        }
                    }
                    finally
                    {
                        await hostingService.StopAsync(cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unhandled error occured.");
                return 1;
            }

            return 0;
        }
    }
}