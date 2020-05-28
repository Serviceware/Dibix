namespace Dibix.Sdk.CodeGeneration
{
    public sealed class LocalActionTarget : GeneratedAccessorMethodTarget
    {
        public SqlStatementInfo Statement { get; }

        public LocalActionTarget(SqlStatementInfo statement, string outputName) : base($"{statement.Namespace}.{outputName}", statement.ResultType, statement.Name)
        {
            this.Statement = statement;
            foreach (SqlQueryParameter parameter in statement.Parameters)
            {
                base.Parameters.Add(parameter.Name, new ActionParameter(parameter.Name, parameter.Type, parameter.HasDefaultValue, parameter.DefaultValue));
            }
        }
    }
}