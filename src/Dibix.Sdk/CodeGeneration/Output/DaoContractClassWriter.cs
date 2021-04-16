namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DaoContractClassWriter : ContractClassWriter
    {
        protected override bool GenerateRuntimeSpecifics => true;
    }
}