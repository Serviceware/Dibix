namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ActionDefinitionTarget
    {
        public string AccessorFullName { get; }
        public string OperationName { get; }
        public bool IsAsync { get; }
        public bool HasRefParameters { get; }
        public string Source { get; }
        public int Line { get; }
        public int Column { get; }

        protected ActionDefinitionTarget(string accessorFullName, string operationName, bool isAsync, bool hasRefParameters, string source, int line, int column)
        {
            this.AccessorFullName = accessorFullName;
            this.OperationName = operationName;
            this.IsAsync = isAsync;
            this.HasRefParameters = hasRefParameters;
            this.Source = source;
            this.Line = line;
            this.Column = column;
        }
    }
}