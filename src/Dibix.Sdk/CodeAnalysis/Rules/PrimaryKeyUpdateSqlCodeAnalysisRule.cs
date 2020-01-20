using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class PrimaryKeyUpdateSqlCodeAnalysisRule : SqlCodeAnalysisRule<PrimaryKeyUpdateSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 36;
        public override string ErrorMessage => "Primary keys should not be updated: {0}";
    }

    public sealed class PrimaryKeyUpdateSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private readonly ICollection<string> _tableVariablePrimaryKeyColumns;

        public PrimaryKeyUpdateSqlCodeAnalysisRuleVisitor()
        {
            this._tableVariablePrimaryKeyColumns = new HashSet<string>();
        }

        public override void ExplicitVisit(TSqlScript node)
        {
            TableVariableVisitor tableVariableVisitor = new TableVariableVisitor();
            node.Accept(tableVariableVisitor);
            this._tableVariablePrimaryKeyColumns.ReplaceWith(tableVariableVisitor.PrimaryKeyColumns);

            base.ExplicitVisit(node);
        }

        public override void Visit(AssignmentSetClause node)
        {
            if (node.Column == null)
                return;

            if (!base.TryGetModelElement(node.Column, out ElementLocation elementLocation))
                return;

            bool isPartOfPrimaryKey = base.Model.IsPartOfPrimaryKey(elementLocation, this.IsPartOfTableVariablePrimaryKey);

            if (isPartOfPrimaryKey)
                base.Fail(node, node.Dump());
        }

        private bool IsPartOfTableVariablePrimaryKey(ElementLocation element)
        {
            if (!element.Identifiers.Any()) 
                return false;

            string key = String.Join(".", element.Identifiers.Skip(element.Identifiers.Count - 2));
            return this._tableVariablePrimaryKeyColumns.Contains(key);
        }

        private sealed class TableVariableVisitor : TSqlFragmentVisitor
        {
            public ICollection<string> PrimaryKeyColumns { get; }

            public TableVariableVisitor() => this.PrimaryKeyColumns = new Collection<string>();

            public override void Visit(DeclareTableVariableBody node)
            {
                UniqueConstraintDefinition primaryKey = node.Definition
                                                            .TableConstraints
                                                            .OfType<UniqueConstraintDefinition>()
                                                            .SingleOrDefault(x => x.IsPrimaryKey);

                if (primaryKey == null)
                    return;

                string tableName = node.VariableName.Value;
                ICollection<string> columns = primaryKey.Columns
                                                        .Select(x => $"{tableName}.{x.Column.GetName().Value}")
                                                        .ToArray();

                this.PrimaryKeyColumns.AddRange(columns);
            }
        }
    }
}