using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ActionTarget
    {
        public string AccessorFullName { get; }
        public string OperationName { get; }
        public bool IsAsync { get; }
        public SourceLocation SourceLocation { get; }

        protected ActionTarget(string accessorFullName, string operationName, bool isAsync, SourceLocation sourceLocation)
        {
            AccessorFullName = accessorFullName;
            OperationName = operationName;
            IsAsync = isAsync;
            SourceLocation = sourceLocation;
        }
    }
}