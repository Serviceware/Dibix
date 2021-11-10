using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ContractReferenceCountCollectionAction : IDelayedContractCollectionAction
    {
        private readonly string _typeName;
        private readonly IDictionary<string, ContractDefinition> _contractMap;

        public ContractReferenceCountCollectionAction(string typeName, IDictionary<string, ContractDefinition> contractMap)
        {
            this._typeName = typeName;
            this._contractMap = contractMap;
        }

        void IDelayedContractCollectionAction.Invoke()
        {
            if (!this._contractMap.TryGetValue(this._typeName, out ContractDefinition contractDefinition))
            {
                // The contract is quite possibly not defined in this project
                return;
            }
            contractDefinition.IsUsed = true;
        }
    }
}