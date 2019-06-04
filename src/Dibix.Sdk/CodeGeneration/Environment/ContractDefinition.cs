namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ContractDefinition
    {
        public string Namespace { get; set; }
        public string DefinitionName { get; }
        public abstract bool IsPrimitive { get; }

        protected ContractDefinition(string @namespace, string definitionName)
        {
            this.Namespace = @namespace;
            this.DefinitionName = definitionName;
        }
    }
}