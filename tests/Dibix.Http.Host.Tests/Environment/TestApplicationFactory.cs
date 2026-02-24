using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Dibix.Testing;
using Dibix.Tests;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Host.Tests
{
    internal sealed class TestApplicationFactory : WebApplicationFactory<Program>
    {
        private static TestApplicationFactory? _instance;
        private readonly string _logOutputPath;

        public static TestApplicationFactory Instance => _instance ?? throw new InvalidOperationException("TestApplicationFactory not initialized");
        public LogMessages Logs { get; } = new LogMessages();

        public static void Initialize(ITestContextFacade testContextFacade)
        {
            string logOutputPath = testContextFacade.AddTestRunFile("dibix-http-host.log");
            _instance = new TestApplicationFactory(logOutputPath);
        }

        public static async Task DisposeIfInitialized()
        {
            if (_instance != null)
                await _instance.DisposeAsync().ConfigureAwait(false);
        }

        private TestApplicationFactory(string logOutputPath)
        {
            _logOutputPath = logOutputPath;
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(ConfigureHostConfiguration);
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // WebApplicationFactory uses 'Development' per default, which adds environment specific configuration, like user secrets
            builder.UseEnvironment("Testing");

            builder.ConfigureLogging(ConfigureLogging);
            builder.ConfigureServices(ConfigureServices);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpApiDiscoveryStrategy, TestHttpApiDiscoveryStrategy>();
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.ValidateLifetime = false;
                options.TokenValidationParameters.RequireSignedTokens = false;
            });
        }

        private void ConfigureLogging(ILoggingBuilder builder)
        {
            builder.AddProvider(new DictionaryLoggerProvider(Logs.All));
            builder.AddProvider(new TestOutputLoggerProvider(_logOutputPath));
        }

        private static void ConfigureHostConfiguration(IConfigurationBuilder builder)
        {
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Debug",
                ["Hosting:ExternalHostName"] = "localhost",
                ["Authentication:Authority"] = "https://localhost",
                ["Database:ConnectionString"] = ContainerServices.Instance.MsSqlServer.ConnectionString,
                ["Hosting:Extension"] = typeof(TestApplicationFactory).Assembly.GetName().Name
            });
        }

        public sealed class LogMessages
        {
            public IDictionary<string, IList<string>> All { get; } = new Dictionary<string, IList<string>>();
            public IList<string> ExceptionHandlerMiddlewareMessages => All.TryGetValue("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", out IList<string>? list) ? list : new List<string>();
        }
    }
}