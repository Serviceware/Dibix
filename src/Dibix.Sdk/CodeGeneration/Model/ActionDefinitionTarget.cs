using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ActionDefinitionTarget
    {
        public string AccessorFullName { get; }
        public string OperationName { get; }
        public TypeReference ResultType { get; }
        public bool IsAsync { get; }
        public abstract ICollection<ErrorResponse> ErrorResponses { get; }

        protected ActionDefinitionTarget(string accessorFullName, string operationName, TypeReference resultType, bool isAsync)
        {
            this.AccessorFullName = accessorFullName;
            this.ResultType = resultType;
            this.OperationName = operationName;
            this.IsAsync = isAsync;
        }
    }
}