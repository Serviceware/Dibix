namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ActionDefinitionTarget
    {
        public string AccessorFullName { get; }
        public string OperationName { get; }
        public bool IsAsync { get; }
        public bool HasRefParameters { get; }

        protected ActionDefinitionTarget(string accessorFullName, string operationName, bool isAsync, bool hasRefParameters)
        {
            this.AccessorFullName = accessorFullName;
            this.OperationName = operationName;
            this.IsAsync = isAsync;
            this.HasRefParameters = hasRefParameters;
        }
    }
}