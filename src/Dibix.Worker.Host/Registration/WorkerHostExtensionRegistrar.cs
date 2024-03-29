﻿using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Dibix.Hosting.Abstractions;
using Dibix.Hosting.Abstractions.Data;
using Dibix.Worker.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dibix.Worker.Host
{
    internal static class WorkerHostExtensionRegistrar
    {
        public static IWorkerHostExtensionRegistrar? Register(HostingOptions options, IServiceCollection services, WorkerDependencyRegistry dependencyRegistry)
        {
            if (String.IsNullOrEmpty(options.Extension))
                return null;

            const string kind = "Worker host extension";
            string currentDirectory = AppContext.BaseDirectory;
            string extensionDirectory = Path.Combine(currentDirectory, "Extension");
            string extensionPath = Path.Combine(extensionDirectory, $"{options.Extension}.dll");
            if (!File.Exists(extensionPath))
                throw new InvalidOperationException($"{kind} not found: {extensionPath}");

            AssemblyLoadContext assemblyLoadContext = new ComponentAssemblyLoadContext(name: $"Dibix {kind}", extensionPath);
            IWorkerHostExtension instance = ExtensionRegistrationUtility.GetExtensionImplementation<IWorkerHostExtension>(extensionPath, kind, assemblyLoadContext);
            WorkerHostExtensionConfigurationBuilder builder = new WorkerHostExtensionConfigurationBuilder(services, dependencyRegistry);
            instance.Register(builder);
            return builder;
        }

        private sealed class WorkerHostExtensionConfigurationBuilder : IWorkerHostExtensionConfigurationBuilder, IWorkerHostExtensionRegistrar
        {
            private const string HostFullName = "Dibix.Worker.Host";
            private readonly IServiceCollection _services;
            private readonly WorkerDependencyRegistry _dependencyRegistry;
            private Func<IWorkerScope, Task>? _onHostStartedExtension;
            private Func<IWorkerScope, Task>? _onHostStoppedExtension;

            public WorkerHostExtensionConfigurationBuilder(IServiceCollection services, WorkerDependencyRegistry dependencyRegistry)
            {
                _services = services;
                _dependencyRegistry = dependencyRegistry;
            }

            IWorkerHostExtensionConfigurationBuilder IWorkerHostExtensionConfigurationBuilder.RegisterService<TService>()
            {
                _services.AddHostedService<TService>();
                return this;
            }

            IWorkerHostExtensionConfigurationBuilder IWorkerHostExtensionConfigurationBuilder.RegisterDependency<TInterface, TImplementation>()
            {
                _services.AddScopedOnce<TInterface, TImplementation>();
                return this;
            }

            IWorkerHostExtensionConfigurationBuilder IWorkerHostExtensionConfigurationBuilder.RegisterDependency<TInterface>(Func<IWorkerDependencyContext, TInterface> factory)
            {
                TInterface CreateInstance(IServiceProvider serviceProvider)
                {
                    IWorkerDependencyContext dependencyContext = serviceProvider.GetRequiredService<IWorkerDependencyContext>();
                    return factory(dependencyContext);
                }
                _services.AddScopedOnce(CreateInstance);
                _dependencyRegistry.Register(typeof(TInterface));
                return this;
            }

            IWorkerHostExtensionConfigurationBuilder IWorkerHostExtensionConfigurationBuilder.ConfigureConnectionString(Func<string?, string?> configure)
            {
                _services.Configure<DatabaseOptions>(x => x.ConnectionString = configure(x.ConnectionString));
                return this;
            }

            IWorkerHostExtensionConfigurationBuilder IWorkerHostExtensionConfigurationBuilder.OnHostStarted(Func<IWorkerDependencyContext, Task> handler)
            {
                _onHostStartedExtension = handler;
                return this;
            }

            IWorkerHostExtensionConfigurationBuilder IWorkerHostExtensionConfigurationBuilder.OnHostStopped(Func<IWorkerDependencyContext, Task> handler)
            {
                _onHostStoppedExtension = handler;
                return this;
            }

            IWorkerHostExtensionConfigurationBuilder IWorkerHostExtensionConfigurationBuilder.OnWorkerStarted(OnWorkerStarted handler)
            {
                _services.Configure<HostedServiceEventOptions>(x => x.OnWorkerStarted = handler);
                return this;
            }

            IWorkerHostExtensionConfigurationBuilder IWorkerHostExtensionConfigurationBuilder.OnServiceBrokerIterationCompleted(OnServiceBrokerIterationCompleted handler)
            {
                _services.Configure<HostedServiceEventOptions>(x => x.OnServiceBrokerIterationCompleted = handler);
                return this;
            }

            async Task IWorkerHostExtensionRegistrar.Configure(IHost host)
            {
                if (_onHostStartedExtension == null)
                    return;

                SubscribeToShutdown(host.Services);
                IWorkerScopeFactory workerScopeFactory = host.Services.GetRequiredService<IWorkerScopeFactory>();
                using IWorkerScope scope = workerScopeFactory.Create(HostFullName);
                await _onHostStartedExtension(scope).ConfigureAwait(false);
            }

            private void SubscribeToShutdown(IServiceProvider services)
            {
                IWorkerScope CreateScope()
                {
                    IWorkerScopeFactory workerScopeFactory = services.GetRequiredService<IWorkerScopeFactory>();
                    IWorkerScope scope = workerScopeFactory.Create(HostFullName);
                    return scope;
                }
                IHostApplicationLifetime hostApplicationLifetime = services.GetRequiredService<IHostApplicationLifetime>();
                hostApplicationLifetime.ApplicationStopped.Register(() => OnHostStopped(CreateScope));
            }

            private void OnHostStopped(Func<IWorkerScope> scopeProvider)
            {
                using IWorkerScope scope = scopeProvider();
                _onHostStoppedExtension?.Invoke(scope);
            }
        }
    }

    internal interface IWorkerHostExtensionRegistrar
    {
        Task Configure(IHost host);
    }
}