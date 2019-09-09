using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractDefinitionResolver : IContractResolver
    {
        #region Fields
        private readonly IContractDefinitionProvider _contractDefinitionProvider;
        #endregion

        #region Constructor
        public ContractDefinitionResolver(IContractDefinitionProvider contractDefinitionProvider)
        {
            this._contractDefinitionProvider = contractDefinitionProvider;
        }
        #endregion

        #region IContractResolver Members
        public ContractInfo ResolveContract(string input, Action<string> errorHandler)
        {
            if (input[0] != '#')
                return null;

            string normalizedInput = input.Substring(1, input.Length - 1);
            if (!this._contractDefinitionProvider.TryGetContract(normalizedInput, out ContractDefinition contractDefinition))
            {
                errorHandler($"Could not resolve contract '{normalizedInput}'");
                return null;
            }

            ContractName contractName = new ContractName(input, $"{contractDefinition.Namespace}.{contractDefinition.DefinitionName}");
            ContractInfo contract = new ContractInfo(contractName, contractDefinition.IsPrimitive);
            if (contractDefinition is ObjectContract objectContract)
            {
                foreach (ObjectContractProperty property in objectContract.Properties)
                    contract.Properties.Add(property.Name);
            }
            return contract;
        }
        #endregion
    }
}