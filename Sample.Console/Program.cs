using MassTransit;
using MassTransit.Definition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Sample.Components.Consumers;
using System.Diagnostics;

namespace Sample.Service
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var builder = new HostBuilder().ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", true);
                config.AddEnvironmentVariables();

                if (args != null)
                    config.AddCommandLine(args);
            })
            .ConfigureServices((hostedContext, services) =>
            {
                services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);

                services.AddMassTransit(cfg => {
                    cfg.AddConsumersFromNamespaceContaining<SubmitOrderConsumer>();
                    cfg.AddBus(ConfigureBus);
                });

                services.AddHostedService<MassTransitConsoleHostedService>();
            })
            .ConfigureLogging((hostedContext, logging) => 
            { });

            if (isService)
                await builder.UseWindowsService().Build().RunAsync();
            else
                await builder.RunConsoleAsync();
        }

        static IBusControl ConfigureBus(IBusRegistrationContext registrationContext)
        {

            return Bus.Factory.CreateUsingRabbitMq(cfg => 
            {
                cfg.ConfigureEndpoints(registrationContext);
            });
        }
    }
}