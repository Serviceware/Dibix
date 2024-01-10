using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Dibix
{
    internal sealed class ClaimParameterSource : ActionParameterSourceDefinition<ClaimParameterSource>, IActionParameterFixedPropertySourceDefinition, IActionParameterExtensibleFixedPropertySourceDefinition
    {
        private static readonly IDictionary<string, string> BuiltInClaimNameMap = new Dictionary<string, string>
        {
            ["UserId"] = ClaimTypes.NameIdentifier,
            ["ClientId"] = "azp", // See Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames
            ["Audiences"] = "aud"
        };

        public override string Name => "CLAIM";
        public ICollection<string> Properties { get; } = BuiltInClaimNameMap.Keys.ToList();

        public void AddProperties(params string[] properties) => Properties.AddRange(properties);

        public static string GetBuiltInClaimTypeOrDefault(string claimName) => BuiltInClaimNameMap.TryGetValue(claimName, out string claimType) ? claimType : claimName;
    }
}