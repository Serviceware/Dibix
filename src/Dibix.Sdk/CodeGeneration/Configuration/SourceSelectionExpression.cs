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
            if (this.Parser == null)
                this.Parser = new NoOpParser();

            if (this.Parser.Formatter == null)
                this.Parser.Formatter = new TakeSourceSqlStatementFormatter();

            return this.CollectStatements();
        }
        #endregion

        #region Abstract Methods
        protected abstract IEnumerable<SqlStatementInfo> CollectStatements();
        #endregion
    }
}