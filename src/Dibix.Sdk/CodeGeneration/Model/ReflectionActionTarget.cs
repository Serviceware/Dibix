using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public class ReflectionActionTarget : ActionDefinitionTarget
    {
        public string AssemblyAndTypeQualifiedMethodName { get; }

        public ReflectionActionTarget(string assemblyAndTypeQualifiedMethodName) : base(assemblyAndTypeQualifiedMethodName.Split(',').First().Split('.').Last())
        {
            this.AssemblyAndTypeQualifiedMethodName = assemblyAndTypeQualifiedMethodName;
        }
    }
}