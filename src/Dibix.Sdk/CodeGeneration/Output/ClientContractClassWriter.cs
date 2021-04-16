namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClientContractClassWriter : ContractClassWriter
    {
        protected override bool GenerateRuntimeSpecifics => false;
    }
}