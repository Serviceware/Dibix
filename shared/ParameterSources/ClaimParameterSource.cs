using System.Collections.Generic;

namespace Dibix
{
    internal sealed class ClaimParameterSource : ActionParameterSourceDefinition<ClaimParameterSource>, IActionParameterFixedPropertySourceDefinition, IActionParameterExtensibleFixedPropertySourceDefinition
    {
        public override string Name => "CLAIM";
        public ICollection<string> Properties { get; } = new HashSet<string>
        {
            "UserId",
            "ClientId"
        };

        public void AddProperties(params string[] properties) => Properties.AddRange(properties);
    }
}