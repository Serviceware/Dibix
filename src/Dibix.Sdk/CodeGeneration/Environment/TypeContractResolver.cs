using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class TypeContractResolver : IContractResolver
    {
        #region Fields
        private readonly IAssemblyLocator _assemblyLocator;
        #endregion

        #region Constructor
        public TypeContractResolver(IAssemblyLocator assemblyLocator)
        {
            this._assemblyLocator = assemblyLocator;
        }
        #endregion

        #region IContractResolver Members
        public ContractInfo ResolveContract(string input, Action<string> errorHandler)
        {
            if (input[0] == '#')
                return null;

            bool isAssemblyQualified = input.IndexOf(',') >= 0;
            return !isAssemblyQualified ? TryLocalType(input) : this.TryForeignType(input, errorHandler);
        }

        private static ContractInfo TryLocalType(string input)
        {
            ContractName name = new ContractName(input);

            // Try CSharp type name first (string => System.String)
            Type type = name.TypeName.ToClrType();
            if (type != null)
                return new ContractInfo(name, true);

            type = Type.GetType(name.TypeName);
            if (type == null)
                return null;

            name.TypeName = type.ToCSharpTypeName();
            return new ContractInfo(name, true);
        }

        private ContractInfo TryForeignType(string input, Action<string> errorHandler)
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