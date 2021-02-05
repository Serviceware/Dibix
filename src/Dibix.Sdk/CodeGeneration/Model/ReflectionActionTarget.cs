namespace Dibix.Sdk.CodeGeneration
{
    public class ReflectionActionTarget : NeighborActionTarget
    {
        public string AssemblyName { get; }

        public ReflectionActionTarget(string assemblyName, string accessorFullName, string operationName, bool isAsync, bool hasRefParameters) : base(accessorFullName, operationName, isAsync, hasRefParameters)
        {
            this.AssemblyName = assemblyName;
        }
    }
}