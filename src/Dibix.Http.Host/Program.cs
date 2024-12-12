using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dibix.Hosting.Abstractions.Data;
using Dibix.Http.Server;
using Dibix.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Dibix.Http.Host
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            bool isDevelopment = builder.Environment.IsDevelopment();
            IConfigurationRoot configuration = builder.Configuration;
            IServiceCollection services = builder.Services;

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
                    .AddSingleton<IEndpointRegistrar, DefaultEndpointRegistrar>()
                    .AddSingleton<IAuthorizationHandlerContextFactory, EndpointAuthorizationHandlerContextFactory>()
                    .AddScoped<IParameterDependencyResolver, ParameterDependencyResolver>()
                    .AddScoped<IHttpActionDelegator, HttpActionDelegator>()
                    .AddScoped<IHttpEndpointMetadataProvider, HttpEndpointMetadataProvider>()
                    .AddTransient<IClaimsTransformation, ComposableClaimsTransformation>()
                    .AddScoped<IJwtAudienceProvider, EndpointJwtAudienceProvider>()
                    .AddScoped<EndpointMetadataContext>()
                    .AddTransient<IPostConfigureOptions<JsonOptions>, JsonPostConfigureOptions>()
                    .AddSingleton<IControllerActivator, NotSupportedControllerActivator>();

            services.AddExceptionHandler<DatabaseAccessExceptionHandler>();
            services.AddProblemDetailsWithMapping()
                    .Map<HttpRequestExecutionException>(x => x.IsClientError, (x, y) =>
                    {
                        x.Status = (int)y.StatusCode;
                        x.Detail = y.ErrorMessage;
                        x.Extensions["code"] = y.ErrorCode;
                    });

            IConfigurationSection hostingConfigurationSection = configuration.GetSection(HostingOptions.ConfigurationSectionName);
            HostingOptions hostingOptions = hostingConfigurationSection.Bind<HostingOptions>();

            services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.ConfigurationSectionName))
                    .Configure<HostingOptions>(hostingConfigurationSection)
                    .Configure<AuthenticationOptions>(configuration.GetSection(AuthenticationOptions.ConfigurationSectionName))
                    .Configure<CorsOptions>(configuration.GetSection(CorsOptions.ConfigurationSectionName))
                    .Configure<HttpLoggingOptions>(configuration.GetSection("HttpLogging"));

            // PoC: Set audience based on request
            /*
            services.AddScoped<IOptionsMonitor<JwtBearerOptions>, OptionsMonitor<JwtBearerOptions>>()
                    .AddScoped<IOptionsMonitorCache<JwtBearerOptions>, OptionsCache<JwtBearerOptions>>()
                    .AddSingleton<IPostConfigureOptions<JwtBearerOptions>, ScopedJwtBearerPostConfigureOptions>()
                    .AddScoped<IOptionsFactory<JwtBearerOptions>, ScopedJwtBearerOptionsFactory>();
            */

            services.AddAuthentication()
                    .AddJwtBearer(x =>
                    {
                        AuthenticationOptions authenticationOptions = configuration.GetSection(AuthenticationOptions.ConfigurationSectionName)
                                                                                   .Bind<AuthenticationOptions>();

                        x.Authority = authenticationOptions.Authority;
                        x.Audience = authenticationOptions.Audience;
                        x.RequireHttpsMetadata = !isDevelopment || Equals(authenticationOptions.Authority?.StartsWith("http:", StringComparison.OrdinalIgnoreCase), false);
                        x.TokenValidationParameters.ValidateAudience = authenticationOptions.ValidateAudience;
                    });

            services.AddAuthorization(x =>
            {
                x.AddPolicy(JwtBearerDefaults.AuthenticationScheme, y => y.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                                                                          .RequireAuthenticatedUser()
                                                                          .Build());
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

            IHttpHostExtensionRegistrar? hostExtensionRegistrar = HttpHostExtensionRegistrar.Register(hostingOptions, services, loggerFactory, configuration);

            WebApplication app = builder.Build();

            ILogger logger = loggerFactory.CreateLogger($"Dibix.Http.Host.{nameof(Program)}");
            logger.LogInformation("Using path base: {pathBase}", !String.IsNullOrEmpty(hostingOptions.BaseAddress) ? hostingOptions.BaseAddress : "/");

            if (!String.IsNullOrWhiteSpace(hostingOptions.BaseAddress))
            {
                app.UsePathBase(hostingOptions.BaseAddress);
            }
            app.UseMiddleware<DiagnosticsMiddleware>();
            app.UseHttpLogging();
            app.UseExceptionHandler();
            app.UseRouting();
            app.UseMiddleware<DatabaseScopeMiddleware>();
            app.UseMiddleware<EndpointMetadataMiddleware>();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHsts();

            if (isDevelopment)
            {
                app.MapGet("/configuration", () => ConfigurationSerializer.DumpConfiguration(configuration));
            }

            app.Services.GetRequiredService<IEndpointRegistrar>().Register(app);

            // DbConnection is registered as a scoped service, because it should stay open for the entire HTTP request and then be disposed.
            // To use a scoped service outside the request, a scope must be created manually.
            // This is a sample for future use.
            /*
            using (IServiceScope serviceScope = app.Services.CreateScope())
            {
                // Connection is created here
                IDatabaseAccessorFactory databaseAccessorFactory = serviceScope.ServiceProvider.GetRequiredService<IDatabaseAccessorFactory>();
                using (IDatabaseAccessor databaseAccessor = databaseAccessorFactory.Create())
                {
                    // Connection will not be disposed
                }
                using (IDatabaseAccessor databaseAccessor = databaseAccessorFactory.Create())
                {
                    // Connection will not be disposed
                }
                
                // Connection will now be disposed
            }
            */

            if (hostExtensionRegistrar != null)
                await hostExtensionRegistrar.Configure(app).ConfigureAwait(false);

            await app.RunAsync().ConfigureAwait(false);
        }
    }
}