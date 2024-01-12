using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Dibix.Http.Host
{
    internal sealed class ScopedJwtBearerOptionsFactory : OptionsFactory<JwtBearerOptions>, IOptionsFactory<JwtBearerOptions>
    {
        private readonly IJwtAudienceProvider _audienceProvider;
        
        public ScopedJwtBearerOptionsFactory(IJwtAudienceProvider audienceProvider, IEnumerable<IConfigureOptions<JwtBearerOptions>> setups, IEnumerable<IPostConfigureOptions<JwtBearerOptions>> postConfigures, IEnumerable<IValidateOptions<JwtBearerOptions>> validations) : base(setups, postConfigures, validations)
        {
            _audienceProvider = audienceProvider;
        }

        JwtBearerOptions IOptionsFactory<JwtBearerOptions>.Create(string name)
        {
            JwtBearerOptions options = Create(name);
            _ = ConfigurationManagerCache.TryAdd(name, options.ConfigurationManager);
            options.TokenValidationParameters.ValidAudiences = _audienceProvider.GetValidAudiences();
            return options;
        }
    }
}