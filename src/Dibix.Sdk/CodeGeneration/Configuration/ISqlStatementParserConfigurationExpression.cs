namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementParserConfigurationExpression
    {
        ISqlStatementParserConfigurationExpression Formatter<TFormatter>() where TFormatter : ISqlStatementFormatter, new();
        //ISqlStatementParserConfigurationExpression Lint(Action<SqlLintConfiguration> configuration);
    }
}