using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Dibix.Hosting.Abstractions;
using Dibix.Hosting.Abstractions.Data;
using Dibix.Http.Host.Runtime;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Host
{
    internal static class HttpHostExtensionRegistrar
    {
        public static IHttpHostExtensionRegistrar? Register(HostingOptions options, IServiceCollection services, ILoggerFactory loggerFactory)
        {
            if (String.IsNullOrEmpty(options.Extension))
                return null;

            const string kind = "Http host extension";
            string currentDirectory = AppContext.BaseDirectory;
            string extensionDirectory = Path.Combine(currentDirectory, "Extension");
            string extensionPath = Path.Combine(extensionDirectory, $"{options.Extension}.dll");
            if (!File.Exists(extensionPath))
                throw new InvalidOperationException($"{kind} not found: {extensionPath}");

            ILogger logger = loggerFactory.CreateLogger($"Dibix.Http.Host.{nameof(HttpHostExtensionRegistrar)}");
            logger.LogInformation("Loaded extension: {extensionPath}", extensionPath);

            AssemblyLoadContext assemblyLoadContext = new ComponentAssemblyLoadContext(name: $"Dibix {kind}", extensionPath);
            IHttpHostExtension instance = ExtensionRegistrationUtility.GetExtensionImplementation<IHttpHostExtension>(extensionPath, kind, assemblyLoadContext);
            HttpHostExtensionConfigurationBuilder builder = new HttpHostExtensionConfigurationBuilder(services);
            instance.Register(builder);
            return builder;
        }

        private sealed class HttpHostExtensionConfigurationBuilder : IHttpHostExtensionConfigurationBuilder, IHttpHostExtensionRegistrar
        {
            private const string HostFullName = "Dibix.Http.Host";
            private readonly IServiceCollection _services;
            private Func<IHttpHostExtensionScope, Task>? _onHostStartedExtension;
            private Func<IHttpHostExtensionScope, Task>? _onHostStoppedExtension;

            public HttpHostExtensionConfigurationBuilder(IServiceCollection services)
            {
                _services = services;
            }

            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.EnableRequestIdentityProvider()
            {
                _services.AddHttpContextAccessor()
                         .AddScoped<IRequestIdentityProvider, RequestIdentityProvider>();
                
                return this;
            }

            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.EnableCustomAuthentication<T>(string schemeName)
            {
                _services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, T>(schemeName, configureOptions: _ => { });
                _services.AddAuthorization(x =>
                {
                    x.AddPolicy(schemeName, y => y.AddAuthenticationSchemes(schemeName)
                                                  .RequireAuthenticatedUser()
                                                  .Build());
                });
                return this;
            }

            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.RegisterDependency<TInterface, TImplementation>()
            {
                _services.AddScoped<TInterface, TImplementation>();
                return this;
            }

            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.ConfigureConnectionString(Func<string?, string?> configure)
            {
                _services.Configure<DatabaseOptions>(x => x.ConnectionString = configure(x.ConnectionString));
                return this;
            }

            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.OnHostStarted(Func<IHttpHostExtensionScope, Task> handler)
            {
                _onHostStartedExtension = handler;
                return this;
            }

            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.OnHostStopped(Func<IHttpHostExtensionScope, Task> handler)
            {
                _onHostStoppedExtension = handler;
                return this;
            }

            async Task IHttpHostExtensionRegistrar.Configure(IHost host)
            {
                if (_onHostStartedExtension == null)
                    return;

                SubscribeToShutdown(host.Services);
                using HttpHostExtensionScope scope = CreateScope(host.Services);
                await _onHostStartedExtension(scope).ConfigureAwait(false);
            }

            private void SubscribeToShutdown(IServiceProvider services)
            {
                IHostApplicationLifetime hostApplicationLifetime = services.GetRequiredService<IHostApplicationLifetime>();
                hostApplicationLifetime.ApplicationStopped.Register(() => OnHostStopped(services));
            }

            private static HttpHostExtensionScope CreateScope(IServiceProvider serviceProvider)
            {
                IServiceScope scope = serviceProvider.CreateScope();
                DatabaseScope databaseScope = scope.ServiceProvider.GetRequiredService<DatabaseScope>();
                databaseScope.InitiatorFullName = HostFullName;
                HttpHostExtensionScope extensionScope = new HttpHostExtensionScope(scope);
                return extensionScope;
            }

            private void OnHostStopped(IServiceProvider serviceProvider)
            {
                using HttpHostExtensionScope scope = CreateScope(serviceProvider);
                _onHostStoppedExtension?.Invoke(scope);
            }
        }

        private sealed class HttpHostExtensionScope : IHttpHostExtensionScope, IDisposable
        {
            private readonly IServiceScope _scope;
            private readonly ILoggerFactory _loggerFactory;
            private IDatabaseAccessorFactory? _databaseAccessorFactory;

            public IDatabaseAccessorFactory DatabaseAccessorFactory => _databaseAccessorFactory ??= _scope.ServiceProvider.GetRequiredService<IDatabaseAccessorFactory>();

            public HttpHostExtensionScope(IServiceScope scope)
            {
                _scope = scope;
                _loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            }

            public ILogger CreateLogger(Type loggerType) => _loggerFactory.CreateLogger(loggerType);

            public void Dispose() => _scope.Dispose();
        }
    }

    internal interface IHttpHostExtensionRegistrar
    {
        Task Configure(IHost host);
    }
}