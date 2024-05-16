namespace Dibix.Sdk.CodeGeneration
{
    public sealed class LocalActionTarget : ActionTarget
    {
        public SqlStatementDefinition SqlStatementDefinition { get; }
        public string ExternalAccessorFullName { get; }

        public LocalActionTarget(SqlStatementDefinition sqlStatementDefinition, string localAccessorFullName, string externalAccessorFullName, string operationName, bool isAsync, bool hasRefParameters, SourceLocation sourceLocation) : base(localAccessorFullName, operationName, isAsync, sourceLocation)
        {
            this.SqlStatementDefinition = sqlStatementDefinition;
            this.ExternalAccessorFullName = externalAccessorFullName;
        }
    }
}