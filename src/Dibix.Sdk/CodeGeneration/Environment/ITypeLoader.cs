using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ITypeLoader
    {
        TypeInfo LoadType(IExecutionEnvironment environment, string typeName, string normalizedTypeName, Action<string> errorHandler);
    }
}