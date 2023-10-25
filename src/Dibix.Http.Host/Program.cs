using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dibix.Hosting.Abstractions.Data;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Dibix.Http.Host
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile($"appsettings.{Environment.MachineName}.json", optional: true, reloadOnChange: true);
            bool isDevelopment = builder.Environment.IsDevelopment();

            ILoggerFactory loggerFactory = LoggerFactory.Create(x => x.AddConfiguration(builder.Configuration));
            IServiceCollection services = builder.Services;

            services.AddSingleton<IDatabaseConnectionFactory, DefaultDatabaseConnectionFactory>()
                    .AddScoped<DbConnection>(x => x.GetRequiredService<IDatabaseConnectionFactory>().Create())
                    .AddScoped<IDatabaseConnectionResolver, DependencyInjectionDatabaseConnectionResolver>()
                    .AddScoped<IDatabaseAccessorFactory, ScopedDatabaseAccessorFactory>()
                    .AddScoped<DatabaseScope>()
                    .AddScoped<CreateDatabaseLogger>(x => () => x.GetRequiredService<ILoggerFactory>().CreateLogger(x.GetRequiredService<DatabaseScope>().InitiatorFullName))
                    .AddSingleton<IDatabaseScopeFactory, DatabaseScopeFactory>()
                    .AddSingleton<HttpApiRegistryFactory>()
                    .AddSingleton<IHttpApiRegistry>(z => z.GetRequiredService<HttpApiRegistryFactory>().Create())
                    .AddSingleton<IEndpointUrlBuilder, AssemblyEndpointConfigurationUrlBuilder>()
                    .AddSingleton<IEndpointMetadataProvider, AssemblyEndpointMetadataProvider>()
                    .AddSingleton<IEndpointImplementationProvider, DefaultEndpointImplementationProvider>()
                    .AddSingleton<IEndpointRegistrar, DefaultEndpointRegistrar>()
                    .AddScoped<IParameterDependencyResolver, ParameterDependencyResolver>()
                    .AddScoped<IHttpActionDelegator, HttpActionDelegator>();

            services.AddEventLogOptions();

            IConfigurationSection hostingConfigurationSection = builder.Configuration.GetSection(HostingOptions.ConfigurationSectionName);
            HostingOptions hostingOptions = hostingConfigurationSection.Bind<HostingOptions>();

            services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.ConfigurationSectionName))
                    .Configure<HostingOptions>(hostingConfigurationSection)
                    .Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.ConfigurationSectionName))
                    .Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.ConfigurationSectionName))
                    .ConfigureTarget<JwtBearerOptions>(builder.Configuration, JwtBearerDefaults.AuthenticationScheme)
                    .MapFrom<AuthenticationOptions>(AuthenticationOptions.ConfigurationSectionName, (from, to) =>
                    {
                        to.Authority = from.Authority;
                        to.TokenValidationParameters.ValidAudience = from.Audience;
                        to.RequireHttpsMetadata = !isDevelopment || from.Authority?.StartsWith("http:", StringComparison.OrdinalIgnoreCase) is null or false;
                        to.TokenValidationParameters.ValidateAudience = from.ValidateAudience;
                    });

            services.AddLogging(x => x.AddSimpleConsole(y => y.TimestampFormat = "\x1B[1'm'\x1B[37'm'[yyyy-MM-dd HH:mm:ss.fff\x1B[39'm'\x1B[22'm'] "));

            services.AddAuthentication()
                    .AddJwtBearer();

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

            HttpHostExtensionRegistrar.Register(hostingOptions, services, loggerFactory);

            WebApplication app = builder.Build();

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
            // To use a scoped service outside of the request, a scope must be created manually.
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

            await app.RunAsync().ConfigureAwait(false);
        }
    }
}