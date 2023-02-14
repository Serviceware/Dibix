using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumSchema : SchemaDefinition
    {
        public bool IsFlaggable { get; internal set; }
        public ICollection<EnumSchemaMember> Members { get; }

        public EnumSchema(string @namespace, string definitionName, SchemaDefinitionSource source, SourceLocation location, bool isFlaggable) : base(@namespace, definitionName, source, location)
        {
            this.IsFlaggable = isFlaggable;
            this.Members = new Collection<EnumSchemaMember>();
        }
    }
}