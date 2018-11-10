using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public abstract class SqlCodeAnalysisRule<TVisitor> : ISqlCodeAnalysisRule where TVisitor : SqlCodeAnalysisRuleVisitor, new()
    {
        public abstract int Id { get; }
        public abstract string ErrorMessage { get; }
        public ICollection<SqlRuleProblem> Errors { get; }

        protected SqlCodeAnalysisRule()
        {
            this.Errors = new Collection<SqlRuleProblem>();
        }

        IEnumerable<SqlRuleProblem> ISqlCodeAnalysisRule.Analyze(SqlRuleExecutionContext context)
        {
            this.Errors.Clear();

            if (context.ScriptFragment != null)
                this.AnalyzeFragment(context);

            this.AnalyzeModel(context.ModelElement);

            return this.Errors;
        }

        protected virtual void AnalyzeModel(TSqlObject model)
        {
        }

        private void AnalyzeFragment(SqlRuleExecutionContext context)
        {
            TVisitor visitor = new TVisitor();
            visitor.ErrorHandler = (fragment, token, args) => this.Fail(fragment ?? context.ScriptFragment, token, context.ModelElement, args);

            // First visit each token
            context.ScriptFragment.Visit(visitor.Visit);

            // Now visit each fragment
            context.ScriptFragment.Accept(visitor);
        }

        private void Fail(TSqlFragment fragment, TSqlParserToken token, TSqlObject model, params object[] args)
        {
            string errorText = $"[{this.Id:d3}] {String.Format(this.ErrorMessage, args)}";

            SqlRuleProblem problem = new SqlRuleProblem(errorText, model, fragment);
            if (token != null)
                problem.SetSourceInformation(new SourceInformation(problem.SourceName, token.Line, token.Column));

            this.Errors.Add(problem);
        }
    }
}