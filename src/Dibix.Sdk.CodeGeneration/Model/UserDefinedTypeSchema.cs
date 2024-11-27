using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeSchema : ObjectSchema
    {
        public string UdtName { get; }

        public UserDefinedTypeSchema(string absoluteNamespace, string relativeNamespace, string definitionName, SchemaDefinitionSource source, SourceLocation location, string udtName, IList<ObjectSchemaProperty> properties) : base(absoluteNamespace, relativeNamespace, definitionName, source, location, properties)
        {
            this.UdtName = udtName;
        }
    }
}