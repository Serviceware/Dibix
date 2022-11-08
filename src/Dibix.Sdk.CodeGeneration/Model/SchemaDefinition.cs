namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SchemaDefinition
    {
        public string Namespace { get; }
        public string DefinitionName { get; }
        public SchemaDefinitionSource Source { get; }
        public string FullName => $"{this.Namespace}.{this.DefinitionName}";

        protected SchemaDefinition(string @namespace, string definitionName, SchemaDefinitionSource source)
        {
            this.Namespace = @namespace;
            this.DefinitionName = definitionName;
            this.Source = source;
        }
    }
}