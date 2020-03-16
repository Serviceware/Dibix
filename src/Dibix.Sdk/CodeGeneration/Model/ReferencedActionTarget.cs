namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ReferencedActionTarget : ActionDefinitionTarget
    {
        public string AccessorFullName { get; }

        protected ReferencedActionTarget(string accessorFullName, string methodName) : base(methodName)
        {
            this.AccessorFullName = accessorFullName;
        }
    }
}