using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public abstract class SqlCodeAnalysisRuleVisitor : TSqlFragmentVisitor
    {
        internal ReportSqlCodeAnalysisError ErrorHandler { get; set; }

        protected internal SqlModel Model { get; set; }
        protected internal SqlCodeAnalysisConfiguration Configuration { get; set; }

        public override void ExplicitVisit(TSqlScript node)
        {
            this.BeginStatement(node);
            base.ExplicitVisit(node);
            this.VisitTokens(node);
            this.EndStatement(node);
        }

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            this.Visit(TableModel.Table, node.SchemaObjectName, node.Definition);
        }

        public override void Visit(CreateTypeTableStatement node) => this.Visit(TableModel.TableType, node.Name, node.Definition);

        protected virtual void BeginStatement(TSqlScript node) { }

        protected virtual void EndStatement(TSqlScript node) { }

        protected virtual void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition) { }

        protected virtual void Visit(TSqlParserToken token) { }

        protected void Fail(TSqlParserToken token, params object[] args) => this.Fail(token.Line, token.Column, args);
        protected void Fail(TSqlFragment fragment, params object[] args) => this.Fail(fragment.StartLine, fragment.StartColumn, args);
        protected void Fail(SourceInformation sourceInformation, params object[] args) => this.Fail(sourceInformation.StartLine, sourceInformation.StartColumn, args);
        private void Fail(int line, int column, params object[] args) => this.ErrorHandler(line, column, args);

        private void VisitTokens(TSqlScript node)
        {
            foreach (TSqlParserToken token in node.AsEnumerable())
                this.Visit(token);
        }
    }
}