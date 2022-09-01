using MassTransit;
using MassTransit.Definition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.Components.Consumers;
using Sample.Components.StateMachines;
using System.Diagnostics;

namespace Sample.Service;

class Program
{
    static async Task Main(string[] args)
    {
        var isService = !(Debugger.IsAttached || args.Contains("--console"));

        var builder = new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", true);
                config.AddEnvironmentVariables();

                if (args != null)
                    config.AddCommandLine(args);
            })
            .ConfigureServices((hostingContext, services) =>
            {
                services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);

                services.AddMassTransit(cfg => {
                    cfg.AddConsumersFromNamespaceContaining<SubmitOrderConsumer>();
                    cfg.AddSagaStateMachine<OrderStateMachine, OrderState>(typeof(OrderStsteMachnieDefinition))
                        .MongoDbRepository(r =>
                        {
                            r.Connection = "mongodb://localhost:27017";
                            r.DatabaseName = "orders";
                        });
                    cfg.AddBus(ConfigureBus);
                });

                services.AddHostedService<MassTransitConsoleHostedService>();
            })
            .ConfigureLogging((hostingContext, logging) => 
            {                    
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            });

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