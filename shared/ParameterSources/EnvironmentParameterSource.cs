using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix
{
    internal sealed class EnvironmentParameterSource : ActionParameterSourceDefinition<EnvironmentParameterSource>, IActionParameterFixedPropertySourceDefinition
    {
        public override string Name => "ENV";
        public ICollection<string> Properties { get; } = new Collection<string>
        {
            "CurrentProcessId",
            "MachineName"
        };
    }
}