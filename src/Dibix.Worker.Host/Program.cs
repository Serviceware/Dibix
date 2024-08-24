using System.Data.Common;
using System.Threading.Tasks;
using Dibix.Hosting.Abstractions.Data;
using Dibix.Worker.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Dibix.Worker.Host
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
            IServiceCollection services = builder.Services;

            void ConfigureLogging(ILoggingBuilder logging)
            {
                logging.AddSimpleConsole(y => y.TimestampFormat = "\x1B[1'm'\x1B[37'm'[yyyy-MM-dd HH:mm:ss.fff\x1B[39'm'\x1B[22'm'] ");
                logging.Configure(y => y.ActivityTrackingOptions = ActivityTrackingOptions.None);
                logging.AddEventLogOptions();
            }

            // Prepare logging, that can be used to log during bootstrapping before the host is built
            using ILoggerFactory loggerFactory = LoggerFactory.Create(logging =>
            {
                logging.AddDefaults(builder.Configuration);
                ConfigureLogging(logging);
            });

            services.AddLogging(ConfigureLogging);

            services.AddSingleton<IDatabaseConnectionFactory, DefaultDatabaseConnectionFactory>()
                    .AddScoped<DbConnection>(x => x.GetRequiredService<IDatabaseConnectionFactory>().Create())
                    .AddScoped<IDatabaseConnectionResolver, DependencyInjectionDatabaseConnectionResolver>()
                    .AddScoped<IDatabaseAccessorFactory, ScopedDatabaseAccessorFactory>()
                    .AddScoped<ServiceBrokerDatabaseAccessorFactory>()
                    .AddScoped<ServiceBrokerMessageReader>()
                    .AddScoped<CreateDatabaseLogger>(x => () => x.GetRequiredService<ILoggerFactory>().CreateLogger(x.GetRequiredService<IWorkerDependencyContext>().InitiatorFullName))
                    .AddScoped<IWorkerDependencyContext>(x => x.GetRequiredService<ServiceProviderWorkerDependencyContext>())
                    .AddScoped<ServiceProviderWorkerDependencyContext>()
                    .AddSingleton<ServiceScopeWorkerScopeFactory>()
                    .AddSingleton<IWorkerScopeFactory>(x => x.GetRequiredService<ServiceScopeWorkerScopeFactory>())
                    .AddSingleton<IServiceBrokerMessageReader, ServiceBrokerMessageReader>()
                    .AddSingleton<IHostedServiceEvents, HostedServiceEvents>()
                    .AddHostedService<DatabaseOptionsMonitor>();

            services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.ConfigurationSectionName));
            HostingOptions hostingOptions = builder.Configuration.GetSection(HostingOptions.ConfigurationSectionName).Bind<HostingOptions>();

            services.AddWindowsService();

            WorkerDependencyRegistry dependencyRegistry = new WorkerDependencyRegistry();
            IWorkerHostExtensionRegistrar? hostExtensionRegistrar = WorkerHostExtensionRegistrar.Register(hostingOptions, services, dependencyRegistry);
            WorkerExtensionRegistrar.Register(hostingOptions, services, dependencyRegistry);
            services.AddSingleton<IWorkerDependencyRegistry>(dependencyRegistry);

            IHost host = builder.Build();

            if (hostExtensionRegistrar != null)
                await hostExtensionRegistrar.Configure(host).ConfigureAwait(false);

            await host.RunAsync().ConfigureAwait(false);
        }
    }
}