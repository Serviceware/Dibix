using System;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.VisualStudio
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