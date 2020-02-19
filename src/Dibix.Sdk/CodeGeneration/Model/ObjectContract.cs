using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ObjectContract : ContractDefinition
    {
        public override bool IsPrimitive => false;
        public string WcfNamespace { get; set; }
        public ICollection<ObjectContractProperty> Properties { get; }

        public ObjectContract(Namespace @namespace, string definitionName) : base(@namespace, definitionName)
        {
            this.Properties = new Collection<ObjectContractProperty>();
        }
    }
}