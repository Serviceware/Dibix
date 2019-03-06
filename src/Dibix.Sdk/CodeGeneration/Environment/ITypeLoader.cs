using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ITypeLoader
    {
        TypeInfo LoadType(TypeName typeName, Action<string> errorHandler);
    }
}