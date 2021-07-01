using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class LocalActionTarget : ActionDefinitionTarget
    {
        public SqlStatementDescriptor Statement { get; }

        public LocalActionTarget(SqlStatementDescriptor statement, string outputName) : base($"{statement.Namespace}.{outputName}", statement.Name, statement.Async, statement.Parameters.Any(x => x.IsOutput))
        {
            this.Statement = statement;
        }
    }
}