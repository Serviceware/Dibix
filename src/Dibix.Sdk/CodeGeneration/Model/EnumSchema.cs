using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumSchema : SchemaDefinition
    {
        public bool IsFlaggable { get; internal set; }
        public ICollection<EnumSchemaMember> Members { get; }

        public EnumSchema(string @namespace, string definitionName, bool isFlaggable) : base(@namespace, definitionName)
        {
            this.IsFlaggable = isFlaggable;
            this.Members = new Collection<EnumSchemaMember>();
        }
    }
}