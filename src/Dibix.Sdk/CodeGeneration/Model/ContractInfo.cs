using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ContractInfo
    {
        public ContractName Name { get; }
        public bool IsPrimitiveType { get; }
        public ICollection<string> Properties { get; }

        public ContractInfo(ContractName name, bool isPrimitiveType)
        {
            this.Name = name;
            this.IsPrimitiveType = isPrimitiveType;
            this.Properties = new Collection<string>();
        }
    }
}