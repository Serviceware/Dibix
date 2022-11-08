namespace Dibix.Sdk.CodeGeneration
{
    public class ReflectionActionTarget : NeighborActionTarget
    {
        public string AssemblyName { get; }

        public ReflectionActionTarget(string assemblyName, string accessorFullName, string operationName, bool isAsync, bool hasRefParameters, string source, int line, int column) : base(accessorFullName, operationName, isAsync, hasRefParameters, source, line, column)
        {
            this.AssemblyName = assemblyName;
        }
    }
}