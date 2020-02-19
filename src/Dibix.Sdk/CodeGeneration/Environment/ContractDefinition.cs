namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ContractDefinition
    {
        public Namespace Namespace { get; }
        public string DefinitionName { get; }
        public abstract bool IsPrimitive { get; }

        protected ContractDefinition(Namespace @namespace, string definitionName)
        {
            this.Namespace = @namespace;
            this.DefinitionName = definitionName;
        }
    }
}