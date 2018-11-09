namespace Dibix.Sdk
{
    public interface ISqlStatementParserConfigurationExpression
    {
        ISqlStatementParserConfigurationExpression Formatter<TFormatter>() where TFormatter : ISqlStatementFormatter, new();
    }
}