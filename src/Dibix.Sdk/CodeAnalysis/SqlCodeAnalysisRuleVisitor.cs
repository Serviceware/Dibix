using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public abstract class SqlCodeAnalysisRuleVisitor : TSqlFragmentVisitor
    {
        internal ReportSqlCodeAnalysisError ErrorHandler { get; set; }

        public virtual void Visit(TSqlParserToken token) { }

        protected void Fail(TSqlParserToken token, params object[] args) => this.Fail(null, token, args);
        protected void Fail(TSqlFragment fragment, params object[] args) => this.Fail(fragment, null, args);
        private void Fail(TSqlFragment fragment, TSqlParserToken token, params object[] args)
        {
            this.ErrorHandler(fragment, token, args);
        }
    }
}