using System;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SourceConfigurationExpression<T> : ISourceConfigurationExpression where T : SourceConfiguration
    {
        #region Properties
        protected T Configuration { get; }
        #endregion

        #region Constructor
        protected SourceConfigurationExpression(T configuration)
        {
            this.Configuration = configuration;
        }
        #endregion

        #region ISourceSelectionExpression Members
        public void SelectParser<TParser>() where TParser : ISqlStatementParser { this.SelectParser<TParser>(null); }
        public void SelectParser<TParser>(Action<ISqlStatementParserConfigurationExpression> configuration) where TParser : ISqlStatementParser
        {
            SqlStatementParserConfigurationExpression expression = new SqlStatementParserConfigurationExpression();
            configuration?.Invoke(expression);

            this.Configuration.Parser = typeof(TParser);
            if (expression.SelectedFormatter != null)
                this.Configuration.Formatter = expression.SelectedFormatter;
        }
        #endregion
    }
}