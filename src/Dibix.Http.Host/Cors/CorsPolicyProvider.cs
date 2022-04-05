using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Dibix.Http.Host
{
    /// <remarks>
    /// Unfortunately, the Cors implementation doesn't honor bindable configuration sources like <see cref="IOptionsMonitor{CorsOptions}"/>.
    /// Therefore we have to apply the changed configuration to the policy, everytime it's resolved.
    /// </remarks>
    internal sealed class CorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly IOptionsMonitor<CorsOptions> _corsOptions;
        private readonly ICorsPolicyProvider _inner;

        public CorsPolicyProvider(IOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions> options, IOptionsMonitor<CorsOptions> corsOptions)
        {
            this._corsOptions = corsOptions;
            this._inner = new DefaultCorsPolicyProvider(options);
        }

        public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
        {
            CorsPolicy? policy = await this._inner.GetPolicyAsync(context, policyName).ConfigureAwait(false);
            if (policy != null && policyName == null)
                ApplyConfiguration(policy, this._corsOptions.CurrentValue);

            return policy;
        }

        private static void ApplyConfiguration(CorsPolicy policy, CorsOptions options)
        {
            ICollection<string>? allowedOrigins = options.AllowedOrigins;
            if (allowedOrigins != null)
                policy.Origins.ReplaceWith(allowedOrigins);
        }
    }
}