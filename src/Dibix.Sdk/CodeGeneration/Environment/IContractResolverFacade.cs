using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IContractResolverFacade
    {
        void RegisterContractResolver(IContractResolver contractResolver);
        ContractInfo ResolveContract(string input, Action<string> errorHandler);
    }
}