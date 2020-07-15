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
        private readonly IDictionary<string, IDictionary<string, DynamicColumn>> _dynamicColumnSourceVariables;

        protected override string ErrorMessageTemplate => "Primary keys should not be updated: {0}";

        public PrimaryKeyUpdateSqlCodeAnalysisRule()
        {
            this._dynamicColumnSourceVariables = new Dictionary<string, IDictionary<string, DynamicColumn>>();
        }

        protected override void BeginStatement(TSqlScript node)
        {
            TableVariableVisitor tableVariableVisitor = new TableVariableVisitor();
            node.Accept(tableVariableVisitor);

            UserDefinedTableTypeVariableVisitor userDefinedTableTypeVariableVisitor = new UserDefinedTableTypeVariableVisitor(base.Model);
            node.Accept(userDefinedTableTypeVariableVisitor);

            this._dynamicColumnSourceVariables.ReplaceWith(tableVariableVisitor.TableVariables
                                    .Concat(userDefinedTableTypeVariableVisitor.UserDefinedTableTypeVariables)
                                                                               .Select(x => new KeyValuePair<string, IDictionary<string, DynamicColumn>>(x.Name, x.Columns.ToDictionary(y => y.Name))));
        }

        public override void Visit(AssignmentSetClause node)
        {
            if (node.Column == null)
                return;

            if (!base.Model.TryGetModelElement(node.Column, out ElementLocation elementLocation))
                return;

            bool? isPartOfPrimaryKey = null;

            // Try dynamic column source variable first (Table variable/User defined table type variable)
            for (int i = 0; i < elementLocation.Identifiers.Count; i++)
            {
                string identifier = elementLocation.Identifiers[i];
                if (!this._dynamicColumnSourceVariables.TryGetValue(identifier, out IDictionary<string, DynamicColumn> tableVariableColumns)) 
                    continue;

                string columnName = elementLocation.Identifiers[i + 1];
                if (tableVariableColumns.TryGetValue(columnName, out DynamicColumn column)
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
            {
                base.LogError(node.Column, "71502", $"Cannot resolve reference to object {String.Join(".", elementLocation.Identifiers)}");
                return;
            }

            if (isPartOfPrimaryKey.Value)
                base.Fail(node, node.Dump());
        }

        private sealed class TableVariableVisitor : TSqlFragmentVisitor
        {
            public ICollection<DynamicColumnSource> TableVariables { get; }

            public TableVariableVisitor() => this.TableVariables = new Collection<DynamicColumnSource>();

            public override void Visit(DeclareTableVariableBody node)
            {
                UniqueConstraintDefinition primaryKey = node.Definition
                                                            .TableConstraints
                                                            .OfType<UniqueConstraintDefinition>()
                                                            .SingleOrDefault(x => x.IsPrimaryKey);

                ICollection<string> primaryKeyColumns = new HashSet<string>();
                if (primaryKey != null)
                    primaryKeyColumns.AddRange(primaryKey.Columns.Select(x => x.Column.GetName().Value));

                DynamicColumnSource table = new DynamicColumnSource(node.VariableName.Value);
                foreach (ColumnDefinition column in node.Definition.ColumnDefinitions)
                {
                    bool isPartOfPrimaryKey = primaryKeyColumns.Contains(column.ColumnIdentifier.Value);
                    table.Columns.Add(new DynamicColumn(column.ColumnIdentifier.Value, isPartOfPrimaryKey));
                }
                this.TableVariables.Add(table);
            }
        }

        private sealed class UserDefinedTableTypeVariableVisitor : TSqlFragmentVisitor
        {
            private readonly SqlModel _model;

            public ICollection<DynamicColumnSource> UserDefinedTableTypeVariables { get; }

            public UserDefinedTableTypeVariableVisitor(SqlModel model)
            {
                this._model = model;
                this.UserDefinedTableTypeVariables = new Collection<DynamicColumnSource>();
            }

            public override void Visit(DeclareVariableElement node)
            {
                if (!this._model.TryGetModelElement(node.DataType, out ElementLocation element)) 
                    return;

                IDictionary<string, bool> columns = this._model.GetUserDefinedTableTypeColumnsWithPrimaryKeyInformation(element);
                if (columns == null)
                    return;

                DynamicColumnSource table = new DynamicColumnSource(node.VariableName.Value);
                foreach (KeyValuePair<string, bool> column in columns) 
                    table.Columns.Add(new DynamicColumn(column.Key, column.Value));

                this.UserDefinedTableTypeVariables.Add(table);
            }
        }

        private sealed class DynamicColumnSource
        {
            public string Name { get; }
            public ICollection<DynamicColumn> Columns { get; }

            public DynamicColumnSource(string name)
            {
                this.Name = name;
                this.Columns = new Collection<DynamicColumn>();
            }
        }

        private sealed class DynamicColumn
        {
            public string Name { get; }
            public bool IsPartOfPrimaryKey { get; }

            public DynamicColumn(string name, bool isPartOfPrimaryKey)
            {
                this.Name = name;
                this.IsPartOfPrimaryKey = isPartOfPrimaryKey;
            }
        }
    }
}