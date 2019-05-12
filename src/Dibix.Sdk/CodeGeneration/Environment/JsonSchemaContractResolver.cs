using System;
using System.Collections.Generic;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class JsonSchemaContractResolver : IContractResolver
    {
        #region Fields
        private readonly IJsonSchemaProvider _jsonSchemaProvider;
        #endregion

        #region Constructor
        public JsonSchemaContractResolver(IJsonSchemaProvider jsonSchemaProvider)
        {
            this._jsonSchemaProvider = jsonSchemaProvider;
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

            if (!this._jsonSchemaProvider.TryGetSchemaDefinition(schemaName, definitionName, out JSchema definitionSchema))
                throw new InvalidOperationException($"Cannot find JSON schema '{normalizedInput}'");

            ContractName contractName = new ContractName(input, definitionSchema.Title);
            ContractInfo contract = new ContractInfo(contractName, false);
            contract.Schema = definitionSchema;
            foreach (KeyValuePair<string, JSchema> property in definitionSchema.Properties)
                contract.Properties.Add(property.Key);

            return contract;
        }
        #endregion
    }
}