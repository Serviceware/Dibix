namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractDefinitionSchemaTypeResolver : SchemaTypeResolver
    {
        public ContractDefinitionSchemaTypeResolver(ISchemaRegistry schemaRegistry, IContractDefinitionProvider contractDefinitionProvider) : base(schemaRegistry, contractDefinitionProvider) { }
    }
}