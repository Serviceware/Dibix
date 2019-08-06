using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public abstract class SqlCodeAnalysisRuleVisitor : TSqlFragmentVisitor
    {
        private readonly IDictionary<string, ConstraintTarget> _constraints;
        private readonly IDictionary<string, IndexTarget> _indexes;
        private readonly ICollection<Table> _tables;

        internal ReportSqlCodeAnalysisError ErrorHandler { get; set; }

        protected SqlCodeAnalysisRuleVisitor()
        {
            this._constraints = new Dictionary<string, ConstraintTarget>();
            this._indexes = new Dictionary<string, IndexTarget>();
            this._tables = new Collection<Table>();
        }

        public override void ExplicitVisit(TSqlScript node)
        {
            this.CollectConstraints(node);
            this.CollectIndexes(node);
            this.CollectTables(node);

            this.VisitConstraints();
            this.VisitIndexes();
            this.VisitTables();

            base.ExplicitVisit(node);

            this.VisitTokens(node);
        }

        protected IEnumerable<Constraint> GetConstraints(SchemaObjectName tableName)
        {
            string key = tableName.ToKey();
            if (!this._constraints.TryGetValue(key, out ConstraintTarget target))
                yield break;

            foreach (Constraint constraint in target.Constraints)
            {
                yield return constraint;
            }
        }

        protected IEnumerable<Index> GetIndexes(SchemaObjectName tableName)
        {
            string key = tableName.ToKey();
            if (!this._indexes.TryGetValue(key, out IndexTarget target))
                yield break;

            foreach (Index index in target.Indexes)
            {
                yield return index;
            }
        }

        protected virtual void Visit(ConstraintTarget target, Constraint constraint) { }

        protected virtual void Visit(IndexTarget target, Index index) { }

        protected virtual void Visit(Table table) { }

        protected virtual void Visit(TSqlParserToken token) { }

        protected void Fail(TSqlParserToken token, params object[] args) => this.Fail(null, token.Line, token.Column, args);
        protected void Fail(TSqlFragment fragment, params object[] args) => this.Fail(fragment, fragment.StartLine, fragment.StartColumn, args);
        private void Fail(TSqlFragment fragment, int line, int column, params object[] args)
        {
            this.ErrorHandler(fragment, line, column, args);
        }

        private void CollectConstraints(TSqlFragment node)
        {
            ConstraintVisitor constraintVisitor = new ConstraintVisitor();
            node.Accept(constraintVisitor);
            this._constraints.AddRange(constraintVisitor.Targets);
        }

        private void CollectIndexes(TSqlFragment node)
        {
            IndexVisitor indexVisitor = new IndexVisitor();
            node.Accept(indexVisitor);
            this._indexes.AddRange(indexVisitor.Targets);
        }

        private void CollectTables(TSqlFragment node)
        {
            TableDefinitionVisitor tableVisitor = new TableDefinitionVisitor();
            node.Accept(tableVisitor);
            this._tables.AddRange(tableVisitor.Tables);
        }
        private void VisitConstraints()
        {
            foreach (ConstraintTarget constraintTarget in this._constraints.Values)
            {
                foreach (Constraint constraint in constraintTarget.Constraints)
                {
                    this.Visit(constraintTarget, constraint);
                }
            }
        }

        private void VisitIndexes()
        {
            foreach (IndexTarget indexTarget in this._indexes.Values)
            {
                foreach (Index index in indexTarget.Indexes)
                {
                    this.Visit(indexTarget, index);
                }
            }
        }

        private void VisitTables()
        {
            foreach (Table table in this._tables)
            {
                this.Visit(table);
            }
        }

        private void VisitTokens(TSqlScript node)
        {
            foreach (TSqlParserToken token in node.AsEnumerable())
                this.Visit(token);
        }
    }
}