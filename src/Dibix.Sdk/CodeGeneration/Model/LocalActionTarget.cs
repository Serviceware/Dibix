namespace Dibix.Sdk.CodeGeneration
{
    public sealed class LocalActionTarget : ReferencedActionTarget
    {
        public SqlStatementInfo Statement { get; }

        public LocalActionTarget(SqlStatementInfo statement, string outputName) : base($"{statement.Namespace}.{outputName}", statement.Name)
        {
            this.Statement = statement;
        }
    }
}