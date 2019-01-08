using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ITypeLoader
    {
        TypeInfo LoadType(IExecutionEnvironment environment, TypeName typeName, Action<string> errorHandler);
    }
}