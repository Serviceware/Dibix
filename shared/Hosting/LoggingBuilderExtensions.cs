using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.EventLog;

namespace Microsoft.Extensions.Logging.Configuration
{
    internal static class LoggingBuilderExtensions
    {
        // Unfortunately the default configuration isn't public and can't be reused.
        // See: Microsoft.Extensions.Hosting.HostingHostBuilderExtensions.AddDefaultServices
        public static ILoggingBuilder AddDefaults(this ILoggingBuilder builder, IConfiguration configuration)
        {
            bool isWindows =
#if NETCOREAPP
                OperatingSystem.IsWindows();
#elif NETFRAMEWORK
                Environment.OSVersion.Platform == PlatformID.Win32NT;
#else
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif

            // IMPORTANT: This needs to be added *before* configuration is loaded, this lets
            // the defaults be overridden by the configuration.
            if (isWindows)
            {
                // Default the EventLogLoggerProvider to warning or above
                builder.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Warning);
            }

            builder.AddConfiguration(configuration.GetSection("Logging"));
#if NETCOREAPP
            if (!OperatingSystem.IsBrowser())
#endif
            {
                builder.AddConsole();
            }
            builder.AddDebug();
            builder.AddEventSourceLogger();

            if (isWindows)
            {
                // Add the EventLogLoggerProvider on windows machines
                builder.AddEventLog();
            }

            builder.Configure(options =>
            {
                options.ActivityTrackingOptions =
                    ActivityTrackingOptions.SpanId |
                    ActivityTrackingOptions.TraceId |
                    ActivityTrackingOptions.ParentId;
            });

            return builder;
        }

        public static ILoggingBuilder AddEventLogOptions(this ILoggingBuilder builder)
        {
            // EventLog settings are not automatically read from appsettings.json
            // See: https://github.com/dotnet/runtime/issues/47303
            IServiceCollection services = builder.Services;
            if (OperatingSystem.IsWindows())
                LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);

            return builder;
        }
    }
}