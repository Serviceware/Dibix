using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public abstract class SqlCodeAnalysisRule : TSqlFragmentVisitor, ISqlCodeAnalysisRule
    {
        #region Fields
        private readonly int _id;
        private readonly Collection<SqlCodeAnalysisError> _errors;
        private SqlCodeAnalysisContext _context;
        #endregion

        #region Properties
        protected abstract string ErrorMessageTemplate { get; }

        protected SqlModel Model => this._context.Model;
        protected string Hash => this._context.Hash;
        protected SqlCodeAnalysisConfiguration Configuration => this._context.Configuration;
        #endregion

        #region Constructor
        protected SqlCodeAnalysisRule()
        {
            this._id = SqlCodeAnalysisRuleMap.GetRuleId(this.GetType());
            this._errors = new Collection<SqlCodeAnalysisError>();
        }
        #endregion

        #region ISqlCodeAnalysisRule Members
        IEnumerable<SqlCodeAnalysisError> ISqlCodeAnalysisRule.Analyze(SqlCodeAnalysisContext context)
        {
            this._context = context;
            context.Fragment.Accept(this);
            return this._errors;
        }
        #endregion

        #region Overrides
        public override void ExplicitVisit(TSqlScript node)
        {
            this.BeginStatement(node);
            base.ExplicitVisit(node);
            this.VisitTokens(node);
            this.EndStatement(node);
        }

        public override void ExplicitVisit(TSqlBatch node)
        {
            this.BeginBatch(node);
            base.ExplicitVisit(node);
            this.EndBatch(node);
        }

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            this.Visit(TableModel.Table, node.SchemaObjectName, node.Definition);
        }

        public override void Visit(CreateTypeTableStatement node) => this.Visit(TableModel.TableType, node.Name, node.Definition);
        #endregion

        #region Protected Methods
        protected virtual void BeginStatement(TSqlScript node) { }

        protected virtual void EndStatement(TSqlScript node) { }

        protected virtual void BeginBatch(TSqlBatch node) { }

        protected virtual void EndBatch(TSqlBatch node) { }

        protected virtual void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition) { }

        protected virtual void Visit(TSqlParserToken token) { }

        protected void Fail(TSqlParserToken token, params object[] args) => this.Fail(token.Line, token.Column, args);
        protected void Fail(TSqlFragment fragment, params object[] args) => this.Fail(fragment.StartLine, fragment.StartColumn, args);
        protected void Fail(SourceInformation sourceInformation, params object[] args) => this.Fail(sourceInformation.StartLine, sourceInformation.StartColumn, args);
        #endregion

        #region Private Methods
        private void Fail(int line, int column, params object[] args)
        {
            string message = String.Format(this.ErrorMessageTemplate, args);
            SqlCodeAnalysisError error = new SqlCodeAnalysisError(this._id, message, line, column);
            this._errors.Add(error);
        }

        private void VisitTokens(TSqlScript node)
        {
            foreach (TSqlParserToken token in node.AsEnumerable())
                this.Visit(token);
        }
        #endregion
    }
}