using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dibix.Hosting.Abstractions.Data;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
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

            ILoggerFactory loggerFactory = LoggerFactory.Create(x => x.AddConfiguration(builder.Configuration));
            IServiceCollection services = builder.Services;

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
            services.AddProblemDetails();

            services.AddEventLogOptions();

            IConfigurationSection hostingConfigurationSection = builder.Configuration.GetSection(HostingOptions.ConfigurationSectionName);
            HostingOptions hostingOptions = hostingConfigurationSection.Bind<HostingOptions>();

            services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.ConfigurationSectionName))
                    .Configure<HostingOptions>(hostingConfigurationSection)
                    .Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.ConfigurationSectionName))
                    .Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.ConfigurationSectionName));

            // PoC: Set audience based on request
            /*
            services.AddScoped<IOptionsMonitor<JwtBearerOptions>, OptionsMonitor<JwtBearerOptions>>()
                    .AddScoped<IOptionsMonitorCache<JwtBearerOptions>, OptionsCache<JwtBearerOptions>>()
                    .AddSingleton<IPostConfigureOptions<JwtBearerOptions>, ScopedJwtBearerPostConfigureOptions>()
                    .AddScoped<IOptionsFactory<JwtBearerOptions>, ScopedJwtBearerOptionsFactory>();
            */

            services.AddLogging(x =>
            {
                x.AddSimpleConsole(y => y.TimestampFormat = "\x1B[1'm'\x1B[37'm'[yyyy-MM-dd HH:mm:ss.fff\x1B[39'm'\x1B[22'm'] ");
                x.Configure(y => y.ActivityTrackingOptions = ActivityTrackingOptions.None);
            });

            services.AddAuthentication()
                    .AddJwtBearer(x =>
                    {
                        AuthenticationOptions authenticationOptions = builder.Configuration
                                                                             .GetSection(AuthenticationOptions.ConfigurationSectionName)
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

            var hostExtensionRegistrar = HttpHostExtensionRegistrar.Register(hostingOptions, services, loggerFactory, builder.Configuration);

            WebApplication app = builder.Build();

            app.UseMiddleware<DiagnosticsMiddleware>();
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
                app.MapGet("/configuration", () => ConfigurationSerializer.DumpConfiguration(builder.Configuration));
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