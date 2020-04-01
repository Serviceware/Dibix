using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class LocalActionTarget : GeneratedAccessorMethodTarget
    {
        public SqlStatementInfo Statement { get; }

        public LocalActionTarget(SqlStatementInfo statement, string outputName) : base($"{statement.Namespace}.{outputName}", statement.Name)
        {
            base.Parameters.AddRange(statement.Parameters.Select(x => x.Name));
            this.Statement = statement;
        }
    }
}