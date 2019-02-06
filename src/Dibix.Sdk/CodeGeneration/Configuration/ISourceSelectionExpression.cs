using System;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISourceSelectionExpression
    {
        void SelectParser<TParser>() where TParser : ISqlStatementParser, new();
        void SelectParser<TParser>(Action<ISqlStatementParserConfigurationExpression> configuration) where TParser : ISqlStatementParser, new();
    }
}