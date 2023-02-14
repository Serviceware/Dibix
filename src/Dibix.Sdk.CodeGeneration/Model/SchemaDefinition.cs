using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SchemaDefinition
    {
        public string Namespace { get; }
        public string DefinitionName { get; }
        public SchemaDefinitionSource Source { get; }
        public SourceLocation Location { get; }
        public ExternalSchemaInfo ExternalSchemaInfo { get; set; }
        public string FullName => $"{Namespace}.{DefinitionName}";

        protected SchemaDefinition(string @namespace, string definitionName, SchemaDefinitionSource source, SourceLocation location)
        {
            Namespace = @namespace;
            DefinitionName = definitionName;
            Source = source;
            Location = location;
        }
    }
}