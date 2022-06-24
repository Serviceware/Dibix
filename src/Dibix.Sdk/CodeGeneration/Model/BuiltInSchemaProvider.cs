using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class BuiltInSchemaProvider : ISchemaProvider
    {
        private const string Source = "<internal>";

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
                    new ObjectSchemaProperty(name: new Token<string>("Type", Source, line: default, column: default), new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false, source: default, line: default, column: default))
                  , new ObjectSchemaProperty(name: new Token<string>("Data", Source, line: default, column: default), new PrimitiveTypeReference(PrimitiveType.Binary, isNullable: false, isEnumerable: false, source: default, line: default, column: default))
                  , new ObjectSchemaProperty(name: new Token<string>("FileName", Source, line: default, column: default), new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false, source: default, line: default, column: default))
                }
            };
            return schema;
        }
    }
}