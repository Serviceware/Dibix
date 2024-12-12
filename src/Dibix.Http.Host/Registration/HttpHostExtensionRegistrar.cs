using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Dibix.Hosting.Abstractions;
using Dibix.Hosting.Abstractions.Data;
using Dibix.Http.Server;
using Dibix.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dibix.Http.Host
{
    internal static class HttpHostExtensionRegistrar
    {
        public static IHttpHostExtensionRegistrar? Register(HostingOptions options, IServiceCollection services, ILoggerFactory loggerFactory, IConfigurationRoot configuration)
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
            HttpHostExtensionConfigurationBuilder builder = new HttpHostExtensionConfigurationBuilder(services, configuration);
            instance.Register(builder);
            return builder;
        }

        private sealed class HttpHostExtensionConfigurationBuilder : IHttpHostExtensionConfigurationBuilder, IHttpHostExtensionRegistrar
        {
            private readonly IServiceCollection _services;
            private Func<IHttpHostExtensionScope, Task>? _onHostStartedExtension;
            private Func<IHttpHostExtensionScope, Task>? _onHostStoppedExtension;

            public IConfigurationRoot Configuration { get; }

            public HttpHostExtensionConfigurationBuilder(IServiceCollection services, IConfigurationRoot configuration)
            {
                _services = services;
                Configuration = configuration;
            }

            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.ConfigureOptions<TOptions>(string sectionName, Action<TOptions, string?>? optionsMonitorSubscriber) where TOptions : class => Configure(Configuration.GetSection(sectionName), optionsMonitorSubscriber);
            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.ConfigureOptions<TOptions>(IConfiguration configuration, Action<TOptions, string?>? optionsMonitorSubscriber) where TOptions : class => Configure(configuration, optionsMonitorSubscriber);

            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.ConfigureJwtBearer(Action<JwtBearerOptions> configure)
            {
                _services.PostConfigure(JwtBearerDefaults.AuthenticationScheme, configure);
                return this;
            }

            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.EnableCustomAuthentication<THandler, TOptions>(string schemeName) => EnableCustomAuthentication<THandler, TOptions>(schemeName, configureOptions: null);
            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.EnableCustomAuthentication<THandler, TOptions>(string schemeName, Action<TOptions>? configureOptions) => EnableCustomAuthentication<THandler, TOptions>(schemeName, configureOptions);

            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.RegisterClaimsTransformer<T>()
            {
                // Unfortunately Microsoft.AspNetCore.Authentication.IClaimsTransformation.TransformAsync doesn't receive HttpContext.
                // Therefore, we have to register the async local, if needed.
                if (typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.Instance).SelectMany(x => x.GetParameters()).Any(x => x.ParameterType == typeof(IHttpContextAccessor)))
                    _services.AddHttpContextAccessor();

                _services.AddScoped<IClaimsTransformer, T>();
                return this;
            }

            IHttpHostExtensionConfigurationBuilder IHttpHostExtensionConfigurationBuilder.ConfigureConnectionString(Func<string?, string?> configure)
            {
                _services.Configure<DatabaseOptions>(x => x.ConnectionString = configure(x.ConnectionString));
                return this;
            }

            public IHttpHostExtensionConfigurationBuilder ConfigureDiagnosticScopeProvider<TProvider>() where TProvider : IDiagnosticScopeProvider, new()
            {
                _services.Configure<DiagnosticsOptions>(x => x.Provider = new TProvider());
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

            IHttpHostExtensionConfigurationBuilder EnableCustomAuthentication<THandler, TOptions>(string schemeName, Action<TOptions>? configureOptions) where THandler : AuthenticationHandler<TOptions> where TOptions : AuthenticationSchemeOptions, new()
            {
                _services.AddAuthentication().AddScheme<TOptions, THandler>(schemeName, configureOptions);
                _services.AddAuthorization(x =>
                {
                    x.AddPolicy(schemeName, y => y.AddAuthenticationSchemes(schemeName)
                                                  .RequireAuthenticatedUser()
                                                  .Build());
                });
                return this;
            }

            private IHttpHostExtensionConfigurationBuilder Configure<TOptions>(IConfiguration configuration, Action<TOptions, string?>? optionsMonitorSubscriber) where TOptions : class
            {
                _services.Configure<TOptions>(configuration);
                if (optionsMonitorSubscriber != null)
                {
                    Func<IServiceProvider, IDisposable?> subscribeToOptionsMonitor = x => x.GetRequiredService<IOptionsMonitor<TOptions>>().OnChange(optionsMonitorSubscriber);
                    _services.Configure<OptionsMonitorSubscriberOptions>(x => x.Subscribers.Add(subscribeToOptionsMonitor));
                    _services.AddHostedService<OptionsMonitorSubscriberService>();
                }
                return this;
            }

            private void SubscribeToShutdown(IServiceProvider services)
            {
                IHostApplicationLifetime hostApplicationLifetime = services.GetRequiredService<IHostApplicationLifetime>();
                hostApplicationLifetime.ApplicationStopped.Register(() => OnHostStopped(services));
            }

            private static HttpHostExtensionScope CreateScope(IServiceProvider serviceProvider)
            {
                IServiceScope scope = serviceProvider.CreateScope();
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