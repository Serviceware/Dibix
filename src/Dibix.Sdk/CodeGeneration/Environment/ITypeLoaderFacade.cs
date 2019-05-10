using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ITypeLoaderFacade
    {
        void RegisterTypeLoader(ITypeLoader typeLoader);
        TypeInfo LoadType(string typeName, Action<string> errorHandler);
    }
}