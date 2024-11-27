namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SchemaDefinition
    {
        public string AbsoluteNamespace { get; }
        public string RelativeNamespace { get; }
        public string DefinitionName { get; }
        public SchemaDefinitionSource Source { get; }
        public SourceLocation Location { get; }
        public ExternalSchemaInfo ExternalSchemaInfo { get; set; }
        public string FullName => $"{AbsoluteNamespace}.{DefinitionName}";
        public int ReferenceCount { get; set; }

        protected SchemaDefinition(string absoluteNamespace, string relativeNamespace, string definitionName, SchemaDefinitionSource source, SourceLocation location)
        {
            AbsoluteNamespace = absoluteNamespace;
            RelativeNamespace = relativeNamespace;
            DefinitionName = definitionName;
            Source = source;
            Location = location;
        }
    }
}