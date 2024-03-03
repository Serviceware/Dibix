using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Dibix
{
    [ActionParameterSource("CLAIM")]
    internal sealed class ClaimParameterSource : ActionParameterSourceDefinition<ClaimParameterSource>, IActionParameterFixedPropertySourceDefinition
    {
        private readonly IDictionary<string, string> _propertyClaimTypeMap = new Dictionary<string, string>
        {
            ["UserId"]    = ClaimTypes.NameIdentifier,
            ["UserName"]  = "preferred_username",
            ["ClientId"]  = "azp", // See Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames
            ["Audiences"] = "aud"
        };
        private readonly IDictionary<string, string> _claimTypePropertyMap;

        public ICollection<string> Properties => _propertyClaimTypeMap.Keys;

        public ClaimParameterSource()
        {
            _claimTypePropertyMap = _propertyClaimTypeMap.ToDictionary(x => x.Value, x => x.Key);
        }

        public void Register(string claimPropertyName, string claimTypeName)
        {
            _propertyClaimTypeMap.Add(claimPropertyName, claimTypeName);
            _claimTypePropertyMap.Add(claimTypeName, claimPropertyName);
        }

        public bool TryGetClaimTypeName(string propertyName, out string claimTypeName) => _propertyClaimTypeMap.TryGetValue(propertyName, out claimTypeName);

        public bool TryGetPropertyName(string claimType, out string propertyName) => _claimTypePropertyMap.TryGetValue(claimType, out propertyName);

        public string GetClaimTypeName(string propertyName) => _propertyClaimTypeMap[propertyName];
    }
}