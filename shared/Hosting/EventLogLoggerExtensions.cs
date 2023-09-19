using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.EventLog;

namespace Microsoft.Extensions.Logging.Configuration
{
    internal static class EventLogLoggerExtensions
    {
        public static IServiceCollection AddEventLogOptions(this IServiceCollection services)
        {
            // EventLog settings are not automatically read from appsettings.json
            // See: https://github.com/dotnet/runtime/issues/47303
            if (OperatingSystem.IsWindows())
                LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);

            return services;
        }
    }
}