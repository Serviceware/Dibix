namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientContractClassWriter : ContractClassWriter
    {
        protected override SchemaDefinitionSource SchemaFilter => SchemaDefinitionSource.Local | SchemaDefinitionSource.Generated | SchemaDefinitionSource.Foreign;
        protected override bool GenerateRuntimeSpecifics => false;

        public ClientContractClassWriter(CodeGenerationModel model) : base(model) { }
    }
}