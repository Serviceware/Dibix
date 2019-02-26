using System;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SourceSelectionExpression<T> : ISourceSelectionExpression where T : SourceSelection
    {
        #region Properties
        public T Configuration { get; }
        #endregion

        #region Constructor
        public SourceSelectionExpression(T configuration)
        {
            this.Configuration = configuration;
        }
        #endregion

        #region ISourceSelectionExpression Members
        public void SelectParser<TParser>() where TParser : ISqlStatementParser, new() { this.SelectParser<TParser>(null); }
        public void SelectParser<TParser>(Action<ISqlStatementParserConfigurationExpression> configuration) where TParser : ISqlStatementParser, new()
        {
            TParser parser = new TParser();
            SqlStatementParserConfigurationExpression expression = new SqlStatementParserConfigurationExpression();
            configuration?.Invoke(expression);

            this.Configuration.Parser = parser;
            this.Configuration.Formatter = expression.SelectedFormatter;
        }
        #endregion
    }
}