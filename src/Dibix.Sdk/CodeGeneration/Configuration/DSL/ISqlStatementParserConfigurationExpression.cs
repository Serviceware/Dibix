namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementParserConfigurationExpression
    {
        ISqlStatementParserConfigurationExpression Formatter<TFormatter>() where TFormatter : ISqlStatementFormatter;
    }
}