using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ActionTarget
    {
        public string AccessorFullName { get; }
        public string OperationName { get; }
        public bool IsAsync { get; }
        public bool HasRefParameters { get; }
        public SourceLocation SourceLocation { get; }

        protected ActionTarget(string accessorFullName, string operationName, bool isAsync, bool hasRefParameters, SourceLocation sourceLocation)
        {
            AccessorFullName = accessorFullName;
            OperationName = operationName;
            IsAsync = isAsync;
            HasRefParameters = hasRefParameters;
            SourceLocation = sourceLocation;
        }
    }
}