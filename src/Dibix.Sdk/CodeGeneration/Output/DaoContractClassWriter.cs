namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoContractClassWriter : ContractClassWriter
    {
        protected override SchemaDefinitionSource SchemaFilter => SchemaDefinitionSource.Local | SchemaDefinitionSource.Generated;
        protected override bool GenerateRuntimeSpecifics => true;

        public DaoContractClassWriter(CodeGenerationModel model) : base(model) { }
    }
}