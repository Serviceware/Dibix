using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dibix.Hosting.Abstractions.Data;
using Dibix.Http.Server;
using Dibix.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.LoggingExtensions;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

namespace Dibix.Http.Host
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            IConfigurationRoot configuration = builder.Configuration;
            IServiceCollection services = builder.Services;

            bool isDevelopment = builder.Environment.IsDevelopment();
            _ = Boolean.TryParse(builder.WebHost.GetSetting("UseIISIntegration"), out bool runningInIIS);
            const string mcpPath = "/mcp";

            void ConfigureLogging(ILoggingBuilder logging)
            {
                logging.AddSimpleConsole(y => y.TimestampFormat = "\x1B[1'm'\x1B[37'm'[yyyy-MM-dd HH:mm:ss.fff]\x1B[39'm'\x1B[22'm' ");
                logging.Configure(y => y.ActivityTrackingOptions = ActivityTrackingOptions.None);
                logging.AddEventLogOptions();
            }

            // Prepare logging, that can be used to log during bootstrapping before the host is built
            using ILoggerFactory loggerFactory = LoggerFactory.Create(logging =>
            {
                logging.AddDefaults(configuration);
                ConfigureLogging(logging);
            });

            services.AddLogging(ConfigureLogging);
            services.AddHttpLoggingWithSensitiveRequestHeaders(configuration);

            services.AddSingleton<IDatabaseConnectionFactory, DefaultDatabaseConnectionFactory>()
                    .AddScoped<DbConnection>(x => x.GetRequiredService<IDatabaseConnectionFactory>().Create())
                    .AddScoped<IDatabaseConnectionResolver, DependencyInjectionDatabaseConnectionResolver>()
                    .AddScoped<IDatabaseAccessorFactory, ScopedDatabaseAccessorFactory>()
                    .AddScoped<DatabaseScope>()
                    .AddScoped<CreateDatabaseLogger>(x => () => x.GetRequiredService<ILoggerFactory>().CreateLogger(x.GetRequiredService<DatabaseScope>().InitiatorFullName))
                    .AddSingleton<HttpApiRegistryFactory>()
                    .AddSingleton<IHttpApiRegistry>(z => z.GetRequiredService<HttpApiRegistryFactory>().Create())
                    .AddSingleton<IEndpointUrlBuilder, AssemblyEndpointConfigurationUrlBuilder>()
                    .AddSingleton<IEndpointMetadataProvider, AssemblyEndpointMetadataProvider>()
                    .AddSingleton<IEndpointImplementationProvider, DefaultEndpointImplementationProvider>()
                    .AddSingleton<IEndpointRegistrar, HttpEndpointRegistrar>()
                    .AddSingleton<IEndpointRegistrar, McpEndpointRegistrar>()
                    .AddSingleton<IAuthorizationHandlerContextFactory, EndpointAuthorizationHandlerContextFactory>()
                    .AddScoped<IParameterDependencyResolver, ParameterDependencyResolver>()
                    .AddScoped<IHttpActionDelegator, HttpActionDelegator>()
                    .AddScoped<IActionNameProvider, ActionNameProvider>()
                    .AddTransient<IClaimsTransformation, ComposableClaimsTransformation>()
                    .AddScoped<IJwtAudienceProvider, EndpointJwtAudienceProvider>()
                    .AddScoped<EndpointMetadataContext>()
                    .AddTransient<IPostConfigureOptions<JsonOptions>, JsonPostConfigureOptions>()
                    .AddSingleton<IControllerActivator, NotSupportedControllerActivator>();

            if (runningInIIS)
                services.AddHostedService<PackageMonitorService>();

            services.AddProblemDetailsWithMapping()
                    .Map<DatabaseAccessException>(x => x.IsClientError, (x, y) =>
                    {
                        x.Detail = y.ErrorMessage;
                        x.Extensions["code"] = y.ErrorCode;
                    })
                    .Map<HttpRequestExecutionException>(x => x.IsClientError, (x, y) =>
                    {
                        x.Detail = y.ErrorMessage;
                        x.Extensions["code"] = y.ErrorCode;
                    });

            IConfigurationSection hostingConfigurationSection = configuration.GetSection(HostingOptions.ConfigurationSectionName);
            IConfigurationSection authenticationConfigurationSection = configuration.GetSection(AuthenticationOptions.ConfigurationSectionName);
            HostingOptions hostingOptions = hostingConfigurationSection.Bind<HostingOptions>();
            AuthenticationOptions authenticationOptions = authenticationConfigurationSection.Bind<AuthenticationOptions>();

            services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.ConfigurationSectionName))
                    .Configure<HostingOptions>(hostingConfigurationSection)
                    .Configure<AuthenticationOptions>(authenticationConfigurationSection)
                    .Configure<CorsOptions>(configuration.GetSection(CorsOptions.ConfigurationSectionName))
                    .Configure<HttpLoggingOptions>(configuration.GetSection("HttpLogging"));

            // PoC: Set audience based on request
            /*
            services.AddScoped<IOptionsMonitor<JwtBearerOptions>, OptionsMonitor<JwtBearerOptions>>()
                    .AddScoped<IOptionsMonitorCache<JwtBearerOptions>, OptionsCache<JwtBearerOptions>>()
                    .AddSingleton<IPostConfigureOptions<JwtBearerOptions>, ScopedJwtBearerPostConfigureOptions>()
                    .AddScoped<IOptionsFactory<JwtBearerOptions>, ScopedJwtBearerOptionsFactory>();
            */

            if (authenticationOptions.Authority == null)
                throw new InvalidOperationException($"Property not configured: {authenticationConfigurationSection.Key}:{nameof(authenticationOptions.Authority)}");

            services.AddAuthentication()
                    .AddJwtBearer(x =>
                    {
                        x.Authority = authenticationOptions.Authority;
                        x.Audience = authenticationOptions.Audience;
                        x.RequireHttpsMetadata = !isDevelopment || Equals(authenticationOptions.Authority?.StartsWith("http:", StringComparison.OrdinalIgnoreCase), false);
                        x.TokenValidationParameters.ValidateAudience = authenticationOptions.ValidateAudience;
                    })
                    .AddScheme<McpAuthenticationOptions, NoPathBaseMcpAuthenticationHandler>(McpAuthenticationDefaults.AuthenticationScheme, McpAuthenticationDefaults.DisplayName, x =>
                    {
                        Uri resource;
                        if (isDevelopment)
                        {
                            string? urls = builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey);
                            if (urls == null)
                                throw new InvalidOperationException($"Host setting not configured: {WebHostDefaults.ServerUrlsKey}");

                            resource = urls.Split(';')
                                           .Select(y => new Uri($"{y}/"))
                                           .OrderByDescending(y => y.Scheme == "http") // Avoid certificate issues in VSCode which is our preferred MCP test client
                                           .First();
                        }
                        else
                        {
                            string externalHostname = hostingOptions.ExternalHostName ?? throw new InvalidOperationException($"Property not configured: {hostingConfigurationSection.Key}:{nameof(hostingOptions.ExternalHostName)}");
                            resource = new Uri($"https://{externalHostname}{hostingOptions.ApplicationBaseAddress?.TrimEnd('/')}/");
                        }

                        x.ResourceMetadata = new ProtectedResourceMetadata
                        {
                            Resource = new Uri(resource, new Uri(mcpPath.TrimStart('/'), UriKind.Relative)),
                            AuthorizationServers = { new Uri(authenticationOptions.Authority) },
                            ScopesSupported = ["openid"],
                            ResourceName = $"{hostingOptions.EnvironmentName ?? "Dibix"} MCP Server",
                        };

                        // VSCode or the Model Context Protocol does not support the .well-known endpoint to be behind a subpath
                        // Error: Invalid discovery URL: expected path to start with /.well-known/oauth-protected-resource
                        // See: https://github.com/microsoft/vscode/issues/256236
                        // Therefore we skip prefixing the endpoint URL with the PathBase, but add it at the end of the path.
                        // i.E. /WebSite/.well-known/oauth-protected-resource is changed to /.well-known/oauth-protected-resource/WebSite
                        // Here the PathBase suffix is added via McpAuthenticationOptions.ResourceMetadataUri.
                        // The prefix is set via NoPathBaseMcpAuthenticationHandler
                        if (hostingOptions.ApplicationBaseAddress != null)
                            x.ResourceMetadataUri = new Uri($"{x.ResourceMetadataUri}{hostingOptions.ApplicationBaseAddress}", UriKind.Relative);
                    });

            services.AddAuthorization(x =>
            {
                x.AddPolicy(JwtBearerDefaults.AuthenticationScheme, y => y.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                                                                          .RequireAuthenticatedUser());
                x.AddPolicy(McpAuthenticationDefaults.AuthenticationScheme, y => y.AddAuthenticationSchemes(McpAuthenticationDefaults.AuthenticationScheme)
                                                                                  .RequireAuthenticatedUser());
            });

            services.AddTransient<ICorsPolicyProvider, CorsPolicyProvider>();
            services.AddCors(x =>
            {
                x.AddDefaultPolicy(y => y.AllowCredentials()
                                         .AllowAnyHeader()
                                         .WithMethods("GET", "POST")
                                       // See additional configuration in CorsPolicyProvider
                                       /*.WithOrigins(corsOptions.AllowedOrigins ?? Array.Empty<string>())*/);
            });
            services.AddHsts(x =>
            {
                x.Preload = true;
                x.IncludeSubDomains = true;
                x.MaxAge = TimeSpan.FromDays(730); // 2 years => https://hstspreload.org/
            });
            services.AddMcpServer()
                    .WithHttpTransport();

            IHttpHostExtensionRegistrar? hostExtensionRegistrar = HttpHostExtensionRegistrar.Register(hostingOptions, services, loggerFactory, configuration);

            WebApplication app = builder.Build();

            ILogger logger = loggerFactory.CreateLogger(typeof(Program));

            logger.LogInformation("Application running at address: {applicationBase address}", hostingOptions.ApplicationBaseAddress ?? "/");
            logger.LogInformation("Additional path prefix: {additionalPathPrefix}", hostingOptions.AdditionalPathPrefix ?? "<none>");

            if (hostingOptions.AdditionalPathPrefix != null)
            {
                app.UsePathBase(hostingOptions.AdditionalPathPrefix);
            }
            app.UseMiddleware<DiagnosticsMiddleware>();
            app.UseHttpLogging();
            app.UseIdentityLogging(configuration);
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                StatusCodeSelector = x => x switch
                {
                    DatabaseAccessException databaseAccessException => (int)databaseAccessException.HttpStatusCode,
                    HttpRequestExecutionException httpRequestExecutionException => (int)httpRequestExecutionException.StatusCode,
                    _ => StatusCodes.Status500InternalServerError
                },
                SuppressDiagnosticsCallback = x => x.Exception switch
                {
                    DatabaseAccessException databaseAccessException => databaseAccessException.IsClientError,
                    HttpRequestExecutionException httpRequestExecutionException => httpRequestExecutionException.IsClientError,
                    _ => false
                }
            });
            app.UseRouting();
            app.UseMiddleware<DatabaseScopeMiddleware>();
            app.UseMiddleware<EndpointMetadataMiddleware>(mcpPath);
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHsts();

            app.MapMcp(mcpPath)
               .RequireAuthorization(McpAuthenticationDefaults.AuthenticationScheme);

            if (isDevelopment)
            {
                app.MapGet("/configuration", () => ConfigurationSerializer.DumpConfiguration(configuration));
            }

            app.Services.GetServices<IEndpointRegistrar>().Each(x => x.Register(app));

            if (hostExtensionRegistrar != null)
                await hostExtensionRegistrar.Configure(app).ConfigureAwait(false);

            await app.RunAsync().ConfigureAwait(false);
        }
    }
}