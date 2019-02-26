using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlStatementParserConfigurationExpression : ISqlStatementParserConfigurationExpression
    {
        #region Properties
        public Type SelectedFormatter { get; set; }
        #endregion

        #region ISqlStatementParserConfigurationExpression Members
        public ISqlStatementParserConfigurationExpression Formatter<TFormatter>() where TFormatter : ISqlStatementFormatter
        {
            this.SelectedFormatter = typeof(TFormatter);
            return this;
        }
        #endregion
    }
}