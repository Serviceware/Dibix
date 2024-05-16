using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class BuiltInSchemaProvider : ISchemaProvider
    {
        private const string Source = "<internal>";

        public static SchemaDefinition FileEntitySchema { get; } = CollectFileEntitySchema();

        public IEnumerable<SchemaDefinition> Collect() { yield return FileEntitySchema; }

        private static SchemaDefinition CollectFileEntitySchema()
        {
            ObjectSchema schema = new ObjectSchema("Dibix", "FileEntity", SchemaDefinitionSource.Internal, new SourceLocation(Source, line: default, column: default), new[]
            {
                new ObjectSchemaProperty(name: new Token<string>("Type", new SourceLocation(Source, line: default, column: default)), new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false, size: null, location: default))
              , new ObjectSchemaProperty(name: new Token<string>("Data", new SourceLocation(Source, line: default, column: default)), new PrimitiveTypeReference(PrimitiveType.Binary, isNullable: false, isEnumerable: false, size: null, location: default))
              , new ObjectSchemaProperty(name: new Token<string>("FileName", new SourceLocation(Source, line: default, column: default)), new PrimitiveTypeReference(PrimitiveType.String, isNullable: false, isEnumerable: false, size: null, location: default))
            });
            return schema;
        }
    }
}