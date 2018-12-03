using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public abstract class SqlCodeAnalysisRule : ISqlCodeAnalysisRule
    {
        public abstract int Id { get; }
        public abstract string ErrorMessage { get; }
        public virtual bool IsEnabled => true;
        public ICollection<SqlCodeAnalysisError> Errors { get; }

        protected SqlCodeAnalysisRule()
        {
            this.Errors = new Collection<SqlCodeAnalysisError>();
        }

        IEnumerable<SqlCodeAnalysisError> ISqlCodeAnalysisRule.Analyze(TSqlObject modelElement, TSqlFragment scriptFragment)
        {
            this.Errors.Clear();

            this.Analyze(modelElement, scriptFragment);

            return this.Errors;
        }

        protected abstract void Analyze(TSqlObject modelElement, TSqlFragment scriptFragment);

        protected void Fail(TSqlObject modelElement, TSqlFragment fragment, int line, int column, params object[] args)
        {
            string errorText = $"[{this.Id:d3}] {String.Format(this.ErrorMessage, args)}";
            SqlCodeAnalysisError problem = new SqlCodeAnalysisError(this.Id, errorText, modelElement, fragment, line, column);
            this.Errors.Add(problem);
        }
    }


    public abstract class SqlCodeAnalysisRule<TVisitor> : SqlCodeAnalysisRule, ISqlCodeAnalysisRule where TVisitor : SqlCodeAnalysisRuleVisitor, new()
    {
        protected override void Analyze(TSqlObject modelElement, TSqlFragment scriptFragment)
        {
            if (scriptFragment == null)
                return;

            TVisitor visitor = new TVisitor();
            visitor.ErrorHandler = (fragment, line, column, args) => base.Fail(modelElement, fragment ?? scriptFragment, line, column, args);

            // First visit each fragment
            scriptFragment.Accept(visitor);

            // Now visit each token
            scriptFragment.Visit(visitor.Visit);
        }
    }
}