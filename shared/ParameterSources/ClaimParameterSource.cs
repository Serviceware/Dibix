using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Dibix
{
    [ActionParameterSource("CLAIM")]
    internal sealed class ClaimParameterSource : ActionParameterSourceDefinition<ClaimParameterSource>, IActionParameterFixedPropertySourceDefinition
    {
        private readonly IDictionary<string, PropertyParameterSourceDescriptor> _claimTypePropertyMap = new Dictionary<string, PropertyParameterSourceDescriptor>
        {
             { ClaimTypes.NameIdentifier , new PropertyParameterSourceDescriptor("UserId", new PrimitiveTypeReference(PrimitiveType.String, isNullable: true, isEnumerable: false)) }
           , { "preferred_username"      , new PropertyParameterSourceDescriptor("UserName", new PrimitiveTypeReference(PrimitiveType.String, isNullable: true, isEnumerable: false)) }
             // See Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames
           , { "azp"                     , new PropertyParameterSourceDescriptor("ClientId", new PrimitiveTypeReference(PrimitiveType.String, isNullable: true, isEnumerable: false)) }
           , { "aud"                     , new PropertyParameterSourceDescriptor("Audiences", new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: true)) }
        };
        private readonly IDictionary<string, PropertyParameterSourceDescriptor> _propertyDescriptorMap;
        private readonly IDictionary<string, string> _propertyClaimTypeMap;

        public ClaimParameterSource()
        {
            _propertyDescriptorMap = _claimTypePropertyMap.Values.ToDictionary(x => x.Name);
            _propertyClaimTypeMap = _claimTypePropertyMap.ToDictionary(x => x.Value.Name, x => x.Key);
        }

        public ICollection<PropertyParameterSourceDescriptor> Properties => _claimTypePropertyMap.Values;

        public void Register(PropertyParameterSourceDescriptor property, string claimTypeName)
        {
            _propertyDescriptorMap.Add(property.Name, property);
            _propertyClaimTypeMap.Add(property.Name, claimTypeName);
            _claimTypePropertyMap.Add(claimTypeName, property);
        }

        public bool TryGetClaimTypeName(string propertyName, out string claimTypeName) => _propertyClaimTypeMap.TryGetValue(propertyName, out claimTypeName);

        public bool TryGetPropertyName(string claimType, out string propertyName)
        {
            if (_claimTypePropertyMap.TryGetValue(claimType, out PropertyParameterSourceDescriptor property))
            {
                propertyName = property.Name;
                return true;
            }
            propertyName = null;
            return false;
        }

        public TypeReference TryGetType(string propertyName) => !_propertyDescriptorMap.TryGetValue(propertyName, out PropertyParameterSourceDescriptor property) ? null : property.Type;

        public string GetClaimTypeName(string propertyName) => _propertyClaimTypeMap[propertyName];
    }
}