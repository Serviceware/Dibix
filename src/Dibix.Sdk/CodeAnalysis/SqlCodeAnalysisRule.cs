using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public abstract class SqlCodeAnalysisRule<TVisitor> : ISqlCodeAnalysisRule where TVisitor : SqlCodeAnalysisRuleVisitor, new()
    {
        public abstract int Id { get; }
        public abstract string ErrorMessage { get; }
        public virtual bool IsEnabled => true;
        public ICollection<SqlCodeAnalysisError> Errors { get; }

        protected SqlCodeAnalysisRule()
        {
            this.Errors = new Collection<SqlCodeAnalysisError>();
        }

        IEnumerable<SqlCodeAnalysisError> ISqlCodeAnalysisRule.Analyze(TSqlFragment scriptFragment)
        {
            this.Errors.Clear();

            this.Analyze(scriptFragment);

            return this.Errors;
        }

        private void Analyze(TSqlFragment scriptFragment)
        {
            TVisitor visitor = new TVisitor();
            visitor.ErrorHandler = (fragment, line, column, args) => this.Fail(fragment ?? scriptFragment, line, column, args);
            scriptFragment.Accept(visitor);
        }

        private void Fail(TSqlFragment fragment, int line, int column, params object[] args)
        {
            string errorText = $"[{this.Id:d3}] {String.Format(this.ErrorMessage, args)}";
            SqlCodeAnalysisError problem = new SqlCodeAnalysisError(this.Id, errorText, fragment, line, column);
            this.Errors.Add(problem);
        }
    }
}