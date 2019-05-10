using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ForeignAssemblyTypeContractResolver : IContractResolver
    {
        #region Fields
        private readonly IAssemblyLocator _assemblyLocator;
        #endregion

        #region Constructor
        public ForeignAssemblyTypeContractResolver(IAssemblyLocator assemblyLocator)
        {
            this._assemblyLocator = assemblyLocator;
        }
        #endregion

        #region IContractResolver Members
        public ContractInfo ResolveContract(string input, Action<string> errorHandler)
        {
            try
            {
                string[] parts = input.Split(',');
                if (parts.Length != 2)
                    return null;

                string typeName = parts[0];
                string assemblyName = parts[1];

                ContractName contractName = new ContractName(typeName);
                if (this._assemblyLocator.TryGetAssemblyLocation(assemblyName, out string assemblyLocation))
                    return ReflectionTypeLoader.GetTypeInfo(assemblyName, contractName, assemblyLocation);

                errorHandler($"Could not locate assembly: {assemblyName}");
                return null;
            }
            catch (Exception ex)
            {
                errorHandler(ex.Message);
                return null;
            }
        }
        #endregion
    }
}