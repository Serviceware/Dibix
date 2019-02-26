namespace Dibix.Sdk.CodeGeneration
{
    internal class SqlStatementParserConfigurationExpression : ISqlStatementParserConfigurationExpression
    {
        #region Properties
        public ISqlStatementFormatter SelectedFormatter { get; set; }
        #endregion

        #region ISqlStatementParserConfigurationExpression Members
        public ISqlStatementParserConfigurationExpression Formatter<TFormatter>() where TFormatter : ISqlStatementFormatter, new()
        {
            this.SelectedFormatter = new TFormatter();
            return this;
        }
        #endregion
    }
}