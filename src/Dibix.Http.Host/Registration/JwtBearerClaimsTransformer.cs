using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dibix.Http.Server;

namespace Dibix.Http.Host
{
    internal sealed class JwtBearerClaimsTransformer : IClaimsTransformer
    {
        private static readonly IDictionary<string, string> ClaimMap = new Dictionary<string, string>
        {
            [ClaimTypes.NameIdentifier] = "UserId"
          , [Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Azp] = "ClientId"
        };

        public Task TransformAsync(ClaimsPrincipal principal)
        {
            ClaimsIdentity identity = new ClaimsIdentity("Dibix JWT Bearer Extension");

            foreach (KeyValuePair<string, string> claimMapping in ClaimMap)
            {
                Claim? claim = principal.Claims.FirstOrDefault(x => x.Type == claimMapping.Key);
                if (claim == null) 
                    continue;

                identity.AddClaim(new Claim(claimMapping.Value, claim.Value));
            }

            principal.AddIdentity(identity);
            return Task.CompletedTask;
        }
    }
}