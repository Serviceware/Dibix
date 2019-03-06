using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ITypeLoaderFacade
    {
        TypeInfo LoadType(string typeName, Action<string> errorHandler);
    }
}