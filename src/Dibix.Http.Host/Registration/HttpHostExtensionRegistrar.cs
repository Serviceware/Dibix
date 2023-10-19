using System;
using System.IO;
using System.Runtime.Loader;
using Dibix.Hosting.Abstractions;
using Dibix.Hosting.Abstractions.Data;
using Dibix.Http.Host.Runtime;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Host
{
    internal static class HttpHostExtensionRegistrar
    {
        public static void Register(HostingOptions options, IServiceCollection services, ILoggerFactory loggerFactory)
        {
            if (String.IsNullOrEmpty(options.Extension))
                return;

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
        }

        private sealed class HttpHostExtensionConfigurationBuilder : IHttpHostExtensionConfigurationBuilder
        {
            private readonly IServiceCollection _services;

            public HttpHostExtensionConfigurationBuilder(IServiceCollection services)
            {
                _services = services;
            }

            public IHttpHostExtensionConfigurationBuilder EnableRequestIdentityProvider()
            {
                _services.AddHttpContextAccessor()
                         .AddScoped<IRequestIdentityProvider, RequestIdentityProvider>();
                
                return this;
            }

            public IHttpHostExtensionConfigurationBuilder EnableCustomAuthentication<T>(Func<HttpActionDefinition, bool>? endpointFilter = null) where T : AuthenticationHandler<AuthenticationSchemeOptions>
            {
                _services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, T>(CustomAuthenticationOptions.SchemeName, configureOptions: _ => { });
                _services.AddOptions<CustomAuthenticationOptions>().Configure(x => x.EndpointFilter = endpointFilter ?? (_ => true));
                _services.AddAuthorization(x =>
                {
                    x.AddPolicy(CustomAuthenticationOptions.SchemeName, y => y.AddAuthenticationSchemes(CustomAuthenticationOptions.SchemeName)
                                                                              .RequireAuthenticatedUser()
                                                                              .Build());
                });
                return this;
            }

            public IHttpHostExtensionConfigurationBuilder RegisterDependency<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface
            {
                _services.AddScoped<TInterface, TImplementation>();
                return this;
            }

            public IHttpHostExtensionConfigurationBuilder ConfigureConnectionString(Func<string?, string?> configure)
            {
                _services.Configure<DatabaseOptions>(x => x.ConnectionString = configure(x.ConnectionString));
                return this;
            }
        }
    }
}