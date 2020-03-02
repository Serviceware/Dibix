using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
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

        IEnumerable<SqlCodeAnalysisError> ISqlCodeAnalysisRule.Analyze(TSqlModel model, TSqlFragment scriptFragment, SqlCodeAnalysisConfiguration configuration)
        {
            this.Errors.Clear();

            this.Analyze(model, scriptFragment, configuration);

            return this.Errors;
        }

        private void Analyze(TSqlModel model, TSqlFragment scriptFragment, SqlCodeAnalysisConfiguration configuration)
        {
            TVisitor visitor = new TVisitor
            {
                Model = new SqlModel(configuration.NamingConventionPrefix, model, scriptFragment),
                Configuration = configuration,
                ErrorHandler = this.Fail
            };
            scriptFragment.Accept(visitor);
        }

        private void Fail(int line, int column, params object[] args)
        {
            string errorText = $"[{this.Id:d3}] {String.Format(this.ErrorMessage, args)}";
            SqlCodeAnalysisError problem = new SqlCodeAnalysisError(this.Id, errorText, line, column);
            this.Errors.Add(problem);
        }
    }
}