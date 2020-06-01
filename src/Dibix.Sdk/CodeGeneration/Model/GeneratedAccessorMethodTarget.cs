using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class GeneratedAccessorMethodTarget : ActionDefinitionTarget
    {
        public string AccessorFullName { get; }
        public TypeReference ResultType { get; }
        public IDictionary<string, ActionParameter> Parameters { get; }
        public abstract ICollection<ErrorResponse> ErrorResponses { get; }

        protected GeneratedAccessorMethodTarget(string accessorFullName, TypeReference resultType, string methodName) : base(methodName)
        {
            this.AccessorFullName = accessorFullName;
            this.ResultType = resultType;
            this.Parameters = new Dictionary<string, ActionParameter>(StringComparer.OrdinalIgnoreCase);
        }
    }
}