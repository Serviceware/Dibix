using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface IContractResolverFacade
    {
        void RegisterContractResolver(IContractResolver contractResolver);
        void RegisterContractResolver(IContractResolver contractResolver, int position);
        ContractInfo ResolveContract(string input, Action<string> errorHandler);
    }
}