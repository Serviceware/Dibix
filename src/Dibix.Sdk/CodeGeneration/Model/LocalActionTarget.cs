using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class LocalActionTarget : ActionDefinitionTarget
    {
        public SqlStatementInfo Statement { get; }

        public LocalActionTarget(SqlStatementInfo statement, string outputName) : base($"{statement.Namespace}.{outputName}", statement.Name, statement.Async, statement.Parameters.Any(x => x.IsOutput))
        {
            this.Statement = statement;
        }
    }
}