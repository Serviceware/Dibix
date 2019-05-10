using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ContractInfo
    {
        public ContractName Name { get; }
        public bool IsPrimitiveType { get; }
        public ICollection<string> Properties { get; }

        // Used for contracts that do not exist yet and need to be generated
        public object Schema { get; set; }

        public ContractInfo(ContractName name, bool isPrimitiveType)
        {
            this.Name = name;
            this.IsPrimitiveType = isPrimitiveType;
            this.Properties = new Collection<string>();
        }
    }
}