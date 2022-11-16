using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SecuritySchemeRequirements
    {
        public bool HasEffectiveRequirements => Requirements.Any(x => x.Scheme != SecuritySchemes.Anonymous);
        public SecuritySchemeOperator Operator { get; }
        public ICollection<SecuritySchemeRequirement> Requirements { get; }

        public SecuritySchemeRequirements(SecuritySchemeOperator @operator)
        {
            Operator = @operator;
            Requirements = new Collection<SecuritySchemeRequirement>();
        }
    }
}