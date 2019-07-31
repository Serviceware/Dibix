namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionParameterComplexSource : ActionParameterSource
    {
        public string ContractName { get; }

        internal ActionParameterComplexSource(string contractName) => this.ContractName = contractName;
    }
}