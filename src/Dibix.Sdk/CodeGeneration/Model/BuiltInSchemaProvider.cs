using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration.Model
{
    internal sealed class BuiltInSchemaProvider : ISchemaProvider
    {
        public static SchemaDefinition FileEntitySchema { get; } = CollectFileEntitySchema();
        public IEnumerable<SchemaDefinition> Schemas
        {
            get
            {
                yield return FileEntitySchema;
            }
        }

        private static SchemaDefinition CollectFileEntitySchema()
        {
            ObjectSchema schema = new ObjectSchema("Dibix", "FileEntity", SchemaDefinitionSource.Internal)
            {
                Properties =
                {
                    new ObjectSchemaProperty("Type", new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false, source: default, line: default, column: default))
                  , new ObjectSchemaProperty("Data", new PrimitiveTypeReference(PrimitiveType.Binary, isNullable: false, isEnumerable: false, source: default, line: default, column: default))
                  , new ObjectSchemaProperty("FileName", new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false, source: default, line: default, column: default))
                }
            };
            return schema;
        }
    }
}