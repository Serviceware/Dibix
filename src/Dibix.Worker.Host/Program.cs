﻿using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dibix.Hosting.Abstractions.Data;
using Dibix.Worker.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

namespace Dibix.Worker.Host
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
            builder.Configuration.AddJsonFile($"appsettings.{Environment.MachineName}.json", optional: true, reloadOnChange: true);

            IServiceCollection services = builder.Services;

            // EventLog settings are not automatically read from appsettings.json
            // See: https://github.com/dotnet/runtime/issues/47303
            if (OperatingSystem.IsWindows())
                LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);

            services.AddSingleton<IDatabaseConnectionFactory, DefaultDatabaseConnectionFactory>()
                    .AddScoped<DbConnection>(x => x.GetRequiredService<IDatabaseConnectionFactory>().Create())
                    .AddScoped<IDatabaseConnectionResolver, DependencyInjectionDatabaseConnectionResolver>()
                    .AddScoped<IDatabaseAccessorFactory, ScopedDatabaseAccessorFactory>()
                    .AddScoped<ServiceBrokerDatabaseAccessorFactory>()
                    .AddScoped<ServiceBrokerMessageReader>()
                    .AddScoped<ServiceScopeWorkerScopeFactory>()
                    .AddScoped<CreateDatabaseLogger>(x => () => x.GetRequiredService<ILoggerFactory>().CreateLogger(x.GetRequiredService<IWorkerDependencyContext>().InitiatorFullName))
                    .AddScoped<IWorkerDependencyContext>(x => x.GetRequiredService<ServiceProviderWorkerDependencyContext>())
                    .AddScoped<ServiceProviderWorkerDependencyContext>()
                    .AddSingleton<IWorkerScopeFactory, ServiceScopeWorkerScopeFactory>()
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