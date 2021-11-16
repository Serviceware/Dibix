namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractDefinitionSchemaTypeResolver : SchemaTypeResolver
    {
        public ContractDefinitionSchemaTypeResolver(IContractDefinitionProvider contractDefinitionProvider) : base(contractDefinitionProvider) { }
    }
}