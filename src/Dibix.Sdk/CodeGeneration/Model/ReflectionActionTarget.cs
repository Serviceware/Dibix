namespace Dibix.Sdk.CodeGeneration
{
    public class ReflectionActionTarget : NeighborActionTarget
    {
        public string AssemblyName { get; }

        public ReflectionActionTarget(string assemblyName, string accessorFullName, string operationName, TypeReference resultType, bool isAsync) : base(accessorFullName, operationName, resultType, isAsync)
        {
            this.AssemblyName = assemblyName;
        }
    }
}