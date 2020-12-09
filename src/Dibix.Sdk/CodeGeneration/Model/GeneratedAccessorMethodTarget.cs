using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class GeneratedAccessorMethodTarget : ActionDefinitionTarget
    {
        public string AccessorFullName { get; }
        public TypeReference ResultType { get; }
        public bool IsAsync { get; }
        public abstract ICollection<ErrorResponse> ErrorResponses { get; }

        protected GeneratedAccessorMethodTarget(string accessorFullName, TypeReference resultType, string operationName, bool isAsync) : base(operationName)
        {
            this.AccessorFullName = accessorFullName;
            this.ResultType = resultType;
            this.IsAsync = isAsync;
        }
    }
}