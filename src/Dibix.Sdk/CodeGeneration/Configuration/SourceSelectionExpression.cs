using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SourceSelectionExpression : ISourceSelectionExpression, ISourceSelection
    {
        #region Properties
        protected ISqlStatementParser Parser { get; private set; }
        #endregion

        #region ISourceSelectionExpression Members
        public void SelectParser<TParser>() where TParser : ISqlStatementParser, new() { this.SelectParser<TParser>(null); }
        public void SelectParser<TParser>(Action<ISqlStatementParserConfigurationExpression> configuration) where TParser : ISqlStatementParser, new()
        {
            TParser parser = new TParser();
            SqlStatementParserConfigurationExpression expression = new SqlStatementParserConfigurationExpression(parser);
            configuration?.Invoke(expression);

            this.Parser = parser;
        }
        #endregion

        #region ISourceSelection Members
        IEnumerable<SqlStatementInfo> ISourceSelection.CollectStatements()
        {
            ISqlStatementParser parser = this.Parser ?? new NoOpParser();

            if (parser.Formatter == null)
                parser.Formatter = new TakeSourceSqlStatementFormatter();

            return this.CollectStatements();
        }
        #endregion

        #region Abstract Methods
        protected abstract IEnumerable<SqlStatementInfo> CollectStatements();
        #endregion
    }
}