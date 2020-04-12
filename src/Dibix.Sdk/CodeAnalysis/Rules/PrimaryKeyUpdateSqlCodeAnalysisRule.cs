using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 36)]
    public sealed class PrimaryKeyUpdateSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private readonly IDictionary<string, IDictionary<string, TableVariableColumn>> _tableVariables;

        protected override string ErrorMessageTemplate => "Primary keys should not be updated: {0}";

        public PrimaryKeyUpdateSqlCodeAnalysisRule()
        {
            this._tableVariables = new Dictionary<string, IDictionary<string, TableVariableColumn>>();
        }

        protected override void BeginStatement(TSqlScript node)
        {
            TableVariableVisitor tableVariableVisitor = new TableVariableVisitor();
            node.Accept(tableVariableVisitor);
            this._tableVariables.ReplaceWith(tableVariableVisitor.TableVariables.Select(x => new KeyValuePair<string, IDictionary<string, TableVariableColumn>>(x.Name, x.Columns.ToDictionary(y => y.Name))));
        }

        public override void Visit(AssignmentSetClause node)
        {
            if (node.Column == null)
                return;

            if (!base.Model.TryGetModelElement(node.Column, out ElementLocation elementLocation))
                return;

            bool? isPartOfPrimaryKey = null;

            // Try table variable source first
            for (int i = 0; i < elementLocation.Identifiers.Count; i++)
            {
                string identifier = elementLocation.Identifiers[i];
                if (!this._tableVariables.TryGetValue(identifier, out IDictionary<string, TableVariableColumn> tableVariableColumns)) 
                    continue;

                string columnName = elementLocation.Identifiers[i + 1];
                if (tableVariableColumns.TryGetValue(columnName, out TableVariableColumn column)
                 || tableVariableColumns.TryGetValue(node.Column.GetName().Value, out column)) // For MERGE statements the column identifier is not reliable
                {
                    isPartOfPrimaryKey = column.IsPartOfPrimaryKey;
                }
                break;
            }

            // 'Should' be a concrecte column
            if (!isPartOfPrimaryKey.HasValue)
                isPartOfPrimaryKey = base.Model.IsPartOfPrimaryKey(elementLocation);

            if (!isPartOfPrimaryKey.HasValue)
                throw new InvalidOperationException($"Could not determine column information for: {node.Column.Dump()}");

            if (isPartOfPrimaryKey.Value)
                base.Fail(node, node.Dump());
        }

        private sealed class TableVariableVisitor : TSqlFragmentVisitor
        {
            public ICollection<TableVariable> TableVariables { get; }

            public TableVariableVisitor() => this.TableVariables = new Collection<TableVariable>();

            public override void Visit(DeclareTableVariableBody node)
            {
                UniqueConstraintDefinition primaryKey = node.Definition
                                                            .TableConstraints
                                                            .OfType<UniqueConstraintDefinition>()
                                                            .SingleOrDefault(x => x.IsPrimaryKey);

                ICollection<string> primaryKeyColumns = new HashSet<string>();
                if (primaryKey != null)
                    primaryKeyColumns.AddRange(primaryKey.Columns.Select(x => x.Column.GetName().Value));

                TableVariable table = new TableVariable(node.VariableName.Value);
                foreach (ColumnDefinition column in node.Definition.ColumnDefinitions)
                {
                    bool isPartOfPrimaryKey = primaryKeyColumns.Contains(column.ColumnIdentifier.Value);
                    table.Columns.Add(new TableVariableColumn(column.ColumnIdentifier.Value, isPartOfPrimaryKey));
                }
                this.TableVariables.Add(table);
            }
        }

        private sealed class TableVariable
        {
            public string Name { get; }
            public ICollection<TableVariableColumn> Columns { get; }

            public TableVariable(string name)
            {
                this.Name = name;
                this.Columns = new Collection<TableVariableColumn>();
            }
        }

        private sealed class TableVariableColumn
        {
            public string Name { get; }
            public bool IsPartOfPrimaryKey { get; }

            public TableVariableColumn(string name, bool isPartOfPrimaryKey)
            {
                this.Name = name;
                this.IsPartOfPrimaryKey = isPartOfPrimaryKey;
            }
        }
    }
}