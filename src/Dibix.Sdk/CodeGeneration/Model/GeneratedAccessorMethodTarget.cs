using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class GeneratedAccessorMethodTarget : ActionDefinitionTarget
    {
        public string AccessorFullName { get; }
        public ICollection<string> Parameters { get; }

        protected GeneratedAccessorMethodTarget(string accessorFullName, string methodName) : base(methodName)
        {
            this.Parameters = new HashSet<string>();
            this.AccessorFullName = accessorFullName;
        }
    }
}