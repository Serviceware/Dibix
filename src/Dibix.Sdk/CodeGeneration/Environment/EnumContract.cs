using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumContract : ContractDefinition
    {
        public override bool IsPrimitive => true;
        public bool IsFlaggable { get; set; }
        public ICollection<EnumContractMember> Members { get; }

        public EnumContract(Namespace @namespace, string definitionName, bool isFlaggable) : base(@namespace, definitionName)
        {
            this.IsFlaggable = isFlaggable;
            this.Members = new Collection<EnumContractMember>();
        }
    }
}