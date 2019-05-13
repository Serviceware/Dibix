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
            string[] parts = normalizedInput.Split('.');
            if (parts.Length != 2)
                return null;

            string schemaName = parts[0];
            string definitionName = parts[1];

            if (!this._contractDefinitionProvider.TryGetContract(schemaName, definitionName, out ContractDefinition contractDefinition))
                throw new InvalidOperationException($"Cannot resolve contract '{normalizedInput}'");

            ContractName contractName = new ContractName(input, normalizedInput);
            ContractInfo contract = new ContractInfo(contractName, false);
            foreach (ContractDefinitionProperty property in contractDefinition.Properties)
                contract.Properties.Add(property.Name);

            return contract;
        }
        #endregion
    }
}