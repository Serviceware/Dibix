namespace Dibix.Sdk.CodeGeneration
{
    public sealed class LocalActionTarget : ActionDefinitionTarget
    {
        public SqlStatementDefinition SqlStatementDefinition { get; }
        public string ExternalAccessorFullName { get; }

        public LocalActionTarget(SqlStatementDefinition sqlStatementDefinition, string localAccessorFullName, string externalAccessorFullName, string operationName, bool isAsync, bool hasRefParameters, string source, int line, int column) : base(localAccessorFullName, operationName, isAsync, hasRefParameters, source, line, column)
        {
            this.SqlStatementDefinition = sqlStatementDefinition;
            this.ExternalAccessorFullName = externalAccessorFullName;
        }
    }
}