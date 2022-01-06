using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal sealed class DynamicPropertyParameterSource : ActionParameterSourceDefinition, IActionParameterFixedPropertySourceDefinition
    {
        public override string Name { get; }
        public string[] Properties { get; }

        public DynamicPropertyParameterSource(string name, IEnumerable<string> properties)
        {
            this.Name = name;
            this.Properties = properties.ToArray();
        }
    }
}