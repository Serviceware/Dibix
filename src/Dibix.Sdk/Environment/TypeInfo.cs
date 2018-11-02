using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk
{
    public class TypeInfo
    {
        public string Name { get; }
        public bool IsPrimitiveType { get; }
        public ICollection<string> Properties { get; }

        public TypeInfo(string name, bool isPrimitiveType)
        {
            this.Name = name;
            this.IsPrimitiveType = isPrimitiveType;
            this.Properties = new Collection<string>();
        }
    }
}