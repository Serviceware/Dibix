using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.VisualStudio
{
    public interface ISqlStatementParserConfigurationExpression
    {
        ISqlStatementParserConfigurationExpression Formatter<TFormatter>() where TFormatter : ISqlStatementFormatter;
    }
}