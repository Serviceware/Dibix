using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ForeignTypeLoader : ITypeLoader
    {
        #region Fields
        private readonly IAssemblyLocator _assemblyLocator;
        #endregion

        #region Constructor
        public ForeignTypeLoader(IAssemblyLocator assemblyLocator)
        {
            this._assemblyLocator = assemblyLocator;
        }
        #endregion

        #region ITypeLoader Members
        public TypeInfo LoadType(TypeName typeName, Action<string> errorHandler)
        {
            try
            {
                if (String.IsNullOrEmpty(typeName.AssemblyName))
                    return null;

                if (this._assemblyLocator.TryGetAssemblyLocation(typeName.AssemblyName, out string assemblyLocation))
                    return ReflectionTypeLoader.GetTypeInfo(typeName, assemblyLocation);

                errorHandler($"Could not locate assembly: {typeName.AssemblyName}");
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