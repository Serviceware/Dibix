using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ContractResolverFacade : IContractResolverFacade
    {
        #region Fields
        private readonly IList<IContractResolver> _contractResolvers;
        #endregion

        #region Constructor
        public ContractResolverFacade() => this._contractResolvers = new Collection<IContractResolver>();
        public ContractResolverFacade(IAssemblyLocator assemblyLocator)
        {
            this._contractResolvers = new Collection<IContractResolver> { new TypeContractResolver(assemblyLocator) };
        }
        #endregion

        #region IContractResolverFacade Members
        public void RegisterContractResolver(IContractResolver contractResolver) => this.RegisterContractResolver(contractResolver, this._contractResolvers.Count);
        public void RegisterContractResolver(IContractResolver contractResolver, int position) => this._contractResolvers.Insert(position, contractResolver);

        public ContractInfo ResolveContract(string input, Action<string> errorHandler)
        {
            ContractInfo contract = this._contractResolvers.Select(x => x.ResolveContract(input, errorHandler)).FirstOrDefault(x => x != null);
            if (contract == null)
                errorHandler($"Could not resolve contract '{input}'");

            return contract;
        }
        #endregion
    }
}