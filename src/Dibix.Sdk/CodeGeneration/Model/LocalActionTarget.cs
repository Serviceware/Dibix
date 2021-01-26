using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class LocalActionTarget : ActionDefinitionTarget
    {
        public SqlStatementInfo Statement { get; }
        public override ICollection<ErrorResponse> ErrorResponses => this.Statement.ErrorResponses;

        public LocalActionTarget(SqlStatementInfo statement, string outputName) : base($"{statement.Namespace}.{outputName}", statement.Name, statement.ResultType, statement.Async)
        {
            this.Statement = statement;
        }
    }
}