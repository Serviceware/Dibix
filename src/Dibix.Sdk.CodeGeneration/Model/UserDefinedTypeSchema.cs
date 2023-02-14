using System.Collections.Generic;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeSchema : ObjectSchema
    {
        public string UdtName { get; }

        public UserDefinedTypeSchema(string @namespace, string definitionName, SchemaDefinitionSource source, SourceLocation location, string udtName, IList<ObjectSchemaProperty> properties) : base(@namespace, definitionName, source, location, properties)
        {
            this.UdtName = udtName;
        }
    }
}