using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Dibix.Http.Host
{
    internal sealed class ScopedJwtBearerPostConfigureOptions : IPostConfigureOptions<JwtBearerOptions>
    {
        public void PostConfigure(string? name, JwtBearerOptions options)
        {
            options.ConfigurationManager = ConfigurationManagerCache.Get(name ?? "");
        }
    }
}