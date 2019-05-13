using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeDefinition
    {
        public string TypeName { get; set; }
        public ICollection<UserDefinedTypeColumn> Columns { get; }

        public UserDefinedTypeDefinition()
        {
            this.Columns = new Collection<UserDefinedTypeColumn>();
        }
    }
}
