using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk
{
    public class TypeInfo
    {
        public string Name { get; private set; }
        public bool IsPrimitiveType { get; private set; }
        public ICollection<string> Properties { get; private set; }

        public TypeInfo(string name, bool isPrimitiveType)
        {
            this.Name = name;
            this.IsPrimitiveType = isPrimitiveType;
            this.Properties = new Collection<string>();
        }
    }
}