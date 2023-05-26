using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using Dibix.Hosting.Abstractions;
using Dibix.Http.Client;
using Dibix.Worker.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using IHttpClientBuilder = Microsoft.Extensions.DependencyInjection.IHttpClientBuilder;

namespace Dibix.Worker.Host
{
    internal static class WorkerExtensionRegistrar
    {
        public static void Register(HostingOptions options, IServiceCollection services, WorkerDependencyRegistry dependencyRegistry)
        {
            if (!options.Workers.Any())
                throw new InvalidOperationException("No workers registered");

            string currentDirectory = AppContext.BaseDirectory;
            string workerDirectory = Path.Combine(currentDirectory, "Workers");
            
            foreach (string name in options.Workers)
            {
                string filePath = Path.Combine(workerDirectory, $"{name}.Worker.dll");
                Register(name, filePath, services, dependencyRegistry);
            }
        }

        private static void Register(string componentName, string filePath, IServiceCollection services, WorkerDependencyRegistry dependencyRegistry)
        {
            const string kind = "Worker extension";
            AssemblyLoadContext assemblyLoadContext = new ComponentAssemblyLoadContext($"Dibix {kind} '{componentName}'", filePath);
            IWorkerExtension instance = ExtensionRegistrationUtility.GetExtensionImplementation<IWorkerExtension>(filePath, kind, assemblyLoadContext);
            WorkerExtensionConfigurationBuilder builder = new WorkerExtensionConfigurationBuilder(instance.GetType().Assembly, componentName, services, dependencyRegistry);
            instance.Register(builder);
        }

        private sealed class WorkerExtensionConfigurationBuilder : IWorkerExtensionConfigurationBuilder
        {
            private readonly Assembly _workerAssembly;
            private readonly string _workerComponentName;
            private readonly IServiceCollection _services;
            private readonly WorkerDependencyRegistry _dependencyRegistry;

            public WorkerExtensionConfigurationBuilder(Assembly workerAssembly, string workerComponentName, IServiceCollection services, WorkerDependencyRegistry dependencyRegistry)
            {
                _workerAssembly = workerAssembly;
                _workerComponentName = workerComponentName;
                _services = services;
                _dependencyRegistry = dependencyRegistry;
            }

            public IWorkerExtensionConfigurationBuilder RegisterService<TService>() where TService : HostedService
            {
                _services.AddHostedService<TService>();
                return this;
            }

            public IWorkerExtensionConfigurationBuilder RegisterDependency<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface
            {
                _services.AddScopedOnce<TInterface, TImplementation>();
                _dependencyRegistry.Register(typeof(TInterface));
                return this;
            }

            public IWorkerExtensionConfigurationBuilder RegisterDependency(Type implementationType)
            {
                _services.AddScopedOnce(implementationType);
                _dependencyRegistry.Register(implementationType);
                return this;
            }

            public IWorkerExtensionConfigurationBuilder RegisterHttpClient(string name) => RegisterHttpClientCore(name, configure: null);
            public IWorkerExtensionConfigurationBuilder RegisterHttpClient(string name, Action<IWorkerHttpClientConfigurationBuilder> configure) => RegisterHttpClientCore(name, configure);

            private IWorkerExtensionConfigurationBuilder RegisterHttpClientCore(string name, Action<IWorkerHttpClientConfigurationBuilder>? configure)
            {
                IHttpClientBuilder httpClientBuilder = _services.AddHttpClient(name, ConfigureHttpClient);
                ConfigureHttpClientDefaults(httpClientBuilder);
                if (configure != null)
                {
                    WorkerHttpClientConfigurationBuilder httpClientConfigurationBuilder = new WorkerHttpClientConfigurationBuilder(httpClientBuilder);
                    configure(httpClientConfigurationBuilder);
                }
                return this;
            }

            private void ConfigureHttpClient(HttpClient client)
            {
                const string processName = "DibixWorkerHost";
                client.AddUserAgent(x => x.FromAssembly(_workerAssembly, _ => $"{processName}-{_workerComponentName}"));
            }

            private void ConfigureHttpClientDefaults(IHttpClientBuilder httpClientBuilder)
            {
                AddHttpMessageHandler<FollowRedirectHttpMessageHandler>(httpClientBuilder);
                AddHttpMessageHandler<TraceProxyHttpMessageHandler>(httpClientBuilder);
                AddHttpMessageHandler<EnsureSuccessStatusCodeHttpMessageHandler>(httpClientBuilder);
                AddHttpMessageHandler<TraceSourceHttpMessageHandler>(httpClientBuilder);
            }

            private static void AddHttpMessageHandler<T>(IHttpClientBuilder httpClientBuilder) where T : DelegatingHandler, new()
            {
                httpClientBuilder.AddHttpMessageHandler(() => new T());
            }
        }

        private sealed class WorkerHttpClientConfigurationBuilder : IWorkerHttpClientConfigurationBuilder
        {
            private readonly IHttpClientBuilder _httpClientBuilder;

            public WorkerHttpClientConfigurationBuilder(IHttpClientBuilder httpClientBuilder)
            {
                _httpClientBuilder = httpClientBuilder;
            }

            public void AddTracer<T>() where T : HttpRequestTracer
            {
                _httpClientBuilder.Services.TryAddScoped<T>();
                _httpClientBuilder.AddHttpMessageHandler(x => new TracingHttpMessageHandler(x.GetRequiredService<T>()));
            }

            public void AddHttpMessageHandler<T>() where T : DelegatingHandler
            {
                _httpClientBuilder.Services.TryAddTransient<T>();
                _httpClientBuilder.AddHttpMessageHandler<T>();
            }
        }
    }
}