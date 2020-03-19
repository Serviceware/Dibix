using System;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.VisualStudio
{
    public interface ISourceConfigurationExpression
    {
        void SelectParser<TParser>() where TParser : ISqlStatementParser;
        void SelectParser<TParser>(Action<ISqlStatementParserConfigurationExpression> configuration) where TParser : ISqlStatementParser;
    }
}