using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IContractResolver
    {
        ContractInfo ResolveContract(string input, Action<string> errorHandler);
    }
}