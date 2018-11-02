using System;

namespace Dibix.Sdk
{
    public interface ITypeLoader
    {
        TypeInfo LoadType(IExecutionEnvironment environment, string typeName, string normalizedTypeName, Action<string> errorHandler);
    }
}