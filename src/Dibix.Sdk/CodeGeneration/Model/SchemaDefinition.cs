namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SchemaDefinition
    {
        public string Namespace { get; }
        public string DefinitionName { get; }
        public string FullName => $"{this.Namespace}.{this.DefinitionName}";

        protected SchemaDefinition(string @namespace, string definitionName)
        {
            this.Namespace = @namespace;
            this.DefinitionName = definitionName;
        }
    }
}