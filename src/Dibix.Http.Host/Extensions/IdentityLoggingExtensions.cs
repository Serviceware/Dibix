using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;

namespace Microsoft.IdentityModel.LoggingExtensions
{
    internal static class IdentityLoggingExtensions
    {
        public static IApplicationBuilder UseIdentityLogging(this IApplicationBuilder app, IConfigurationRoot configuration)
        {
            ILoggerFactory loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger("Microsoft.IdentityModel.Logging.Adapter");
            LogHelper.Logger = new IdentityLoggerAdapter(logger);

            if (Boolean.TryParse(configuration["Logging:ShowPII"], out bool showPII) && showPII)
                IdentityModelEventSource.ShowPII = true;

            return app;
        }
    }
}