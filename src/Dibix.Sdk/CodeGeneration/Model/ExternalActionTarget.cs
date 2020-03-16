namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ExternalActionTarget : ReferencedActionTarget
    {
        public ExternalActionTarget(string assemblyAndTypeQualifiedMethodName, string methodName) : base(assemblyAndTypeQualifiedMethodName, methodName)
        {
        }
    }
}