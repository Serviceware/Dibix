using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISourceConfigurationExpression
    {
        void SelectParser<TParser>() where TParser : ISqlStatementParser;
        void SelectParser<TParser>(Action<ISqlStatementParserConfigurationExpression> configuration) where TParser : ISqlStatementParser;
    }
}