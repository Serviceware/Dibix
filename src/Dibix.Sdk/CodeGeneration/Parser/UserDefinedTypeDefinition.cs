using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeDefinition
    {
        public string TypeName { get; }
        public Namespace Namespace { get; set; }
        public string DisplayName { get; }
        public ICollection<UserDefinedTypeColumn> Columns { get; }

        public UserDefinedTypeDefinition(string typeName, Namespace @namespace, string displayName)
        {
            this.TypeName = typeName;
            this.Namespace = @namespace;
            this.DisplayName = displayName;
            this.Columns = new Collection<UserDefinedTypeColumn>();
        }
    }
}
