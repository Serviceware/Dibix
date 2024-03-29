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
        private readonly string _name;
        private readonly int _id;
        private readonly Collection<SqlCodeAnalysisError> _errors;
        private readonly SqlCodeAnalysisContext _context;
        #endregion

        #region Properties
        protected abstract string ErrorMessageTemplate { get; }

        protected SqlModel Model => _context.Model;
        protected SqlCodeAnalysisConfiguration Configuration => _context.Configuration;
        #endregion

        #region Constructor
        protected SqlCodeAnalysisRule(SqlCodeAnalysisContext context)
        {
            _context = context;
            Type type = GetType();
            _name = type.Name;
            _id = SqlCodeAnalysisRuleMap.GetRuleId(type);
            _errors = new Collection<SqlCodeAnalysisError>();
        }
        #endregion

        #region ISqlCodeAnalysisRule Members
        IEnumerable<SqlCodeAnalysisError> ISqlCodeAnalysisRule.Analyze(TSqlFragment fragment)
        {
            fragment.Accept(this);
            return _errors;
        }
        #endregion

        #region Overrides
        public override void ExplicitVisit(TSqlScript node)
        {
            BeginStatement(node);
            base.ExplicitVisit(node);
            VisitTokens(node);
            EndStatement(node);
        }

        public override void ExplicitVisit(TSqlBatch node)
        {
            BeginBatch(node);
            base.ExplicitVisit(node);
            EndBatch(node);
        }

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            Visit(TableModel.Table, node.SchemaObjectName, node.Definition);
        }

        public override void Visit(CreateTypeTableStatement node) => Visit(TableModel.TableType, node.Name, node.Definition);
        #endregion

        #region Protected Methods
        protected virtual void BeginStatement(TSqlScript node) { }

        protected virtual void EndStatement(TSqlScript node) { }

        protected virtual void BeginBatch(TSqlBatch node) { }

        protected virtual void EndBatch(TSqlBatch node) { }

        protected virtual void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition) { }

        protected virtual void Visit(TSqlParserToken token) { }

        protected void LogError(TSqlFragment fragment, string code, string text) => _context.LogError(code, text, fragment.StartLine, fragment.StartColumn);

        protected void Fail(TSqlParserToken token, params object[] args) => Fail(token.Line, token.Column, args);
        protected void Fail(TSqlFragment fragment, params object[] args) => Fail(fragment.StartLine, fragment.StartColumn, args);
        protected void Fail(SourceInformation sourceInformation, params object[] args) => Fail(sourceInformation.StartLine, sourceInformation.StartColumn, args);

        protected void FailIfUnsuppressed(TSqlFragment fragment, string suppressionKey, params object[] args)
        {
            if (_context.IsSuppressed(_name, suppressionKey))
                return;

            Fail(fragment, args);
        }
        protected void FailIfUnsuppressed(SourceInformation source, string suppressionKey, params object[] args)
        {
            if (_context.IsSuppressed(_name, suppressionKey))
                return;

            Fail(source, args);
        }

        //protected bool IsSuppressed(string key) => _context.IsSuppressed(_name, key);
        #endregion

        #region Private Methods
        private void Fail(int line, int column, params object[] args)
        {
            string message = String.Format(ErrorMessageTemplate, args);
            SqlCodeAnalysisError error = new SqlCodeAnalysisError(_id, message, line, column);
            _errors.Add(error);
        }

        private void VisitTokens(TSqlScript node)
        {
            foreach (TSqlParserToken token in node.AsEnumerable())
                Visit(token);
        }
        #endregion
    }
}