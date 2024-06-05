namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ActionTarget
    {
        public string AccessorFullName { get; }
        public string OperationName { get; }
        public string RelativeNamespace { get; }
        public bool IsAsync { get; }
        public SourceLocation SourceLocation { get; }

        protected ActionTarget(string accessorFullName, string operationName, string relativeNamespace, bool isAsync, SourceLocation sourceLocation)
        {
            AccessorFullName = accessorFullName;
            OperationName = operationName;
            RelativeNamespace = relativeNamespace;
            IsAsync = isAsync;
            SourceLocation = sourceLocation;
        }
    }
}