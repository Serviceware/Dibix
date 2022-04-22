namespace Dibix.Sdk.CodeGeneration
{
    public sealed class LocalActionTarget : ActionDefinitionTarget
    {
        public string ExternalAccessorFullName { get; }

        public LocalActionTarget(string localAccessorFullName, string externalAccessorFullName, string operationName, bool isAsync, bool hasRefParameters) : base(localAccessorFullName, operationName, isAsync, hasRefParameters)
        {
            this.ExternalAccessorFullName = externalAccessorFullName;
        }
    }
}