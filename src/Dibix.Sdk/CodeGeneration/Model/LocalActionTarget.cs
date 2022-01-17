namespace Dibix.Sdk.CodeGeneration
{
    public sealed class LocalActionTarget : ActionDefinitionTarget
    {
        public LocalActionTarget(string accessorFullName, string operationName, bool isAsync, bool hasRefParameters) : base(accessorFullName, operationName, isAsync, hasRefParameters)
        {
        }
    }
}