using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeSchema : ObjectSchema
    {
        public string UdtName { get; }

        public UserDefinedTypeSchema(string @namespace, string definitionName, SchemaDefinitionSource source, string udtName, IList<ObjectSchemaProperty> properties) : base(@namespace, definitionName, source, properties)
        {
            this.UdtName = udtName;
        }
    }
}