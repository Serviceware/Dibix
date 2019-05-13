using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ContractDefinition
    {
        public string Namespace { get; set; }
        public string DefinitionName { get; }
        public ICollection<ContractDefinitionProperty> Properties { get; }

        public ContractDefinition(string @namespace, string definitionName)
        {
            this.Namespace = @namespace;
            this.DefinitionName = definitionName;
            this.Properties = new Collection<ContractDefinitionProperty>();
        }
    }
}