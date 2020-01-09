using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    // Disabled, because it's not stable enough
    // 1. As with most other potential rules, it's still hard to determine the actual type of a column expression
    // 2. We can not clearly detect a single row equality WHERE expression 
    public sealed class MisusedTopRowFilterSqlCodeAnalysisRule : SqlCodeAnalysisRule<MisusedTopRowFilterSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 22;
        public override string ErrorMessage => "{0}";
        public override bool IsEnabled => false;
    }

    public sealed class MisusedTopRowFilterSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private readonly IDictionary<string, TableVariable> _tableVariablesWithKey;

        public MisusedTopRowFilterSqlCodeAnalysisRuleVisitor()
        {
            this._tableVariablesWithKey = new Dictionary<string, TableVariable>();
        }

        // Visit whole statement before this visitor
        public override void ExplicitVisit(TSqlScript node)
        {
            TableVariableWithKeyVisitor tableVariableVisitor = new TableVariableWithKeyVisitor();
            node.AcceptChildren(tableVariableVisitor);

            this._tableVariablesWithKey.AddRange(tableVariableVisitor.TableVariables.ToDictionary(x => x.Name));

            base.ExplicitVisit(node);
        }

        public override void Visit(QuerySpecification node)
        {
            if (node.TopRowFilter == null) 
                return;

            if (node.WhereClause != null
             && node.FromClause != null
             && node.FromClause.TableReferences.Any()
             && this.IsSingleEqualityCondition(node.FromClause.TableReferences[0], node.WhereClause))
            {
                base.Fail(node.TopRowFilter, "Invalid TOP filter for single row equality statement");
                return;
            }

            if (node.OrderByClause == null) 
                base.Fail(node.TopRowFilter, "Missing ORDER BY for SELECT TOP statement");
        }

        private bool IsSingleEqualityCondition(TableReference tableReference, WhereClause whereClause)
        {
            if (tableReference is QualifiedJoin joinReference)
            {
                return IsSingleEqualityConditionCore(joinReference.FirstTableReference, whereClause)
                    || IsSingleEqualityConditionCore(joinReference.SecondTableReference, whereClause);
            }

            return IsSingleEqualityConditionCore(tableReference, whereClause);
        }

        private bool IsSingleEqualityConditionCore(TableReference tableReference, WhereClause whereClause)
        {
            ICollection<string> primaryKeyColumns = this.DeterminePrimaryKeyColumns(tableReference).ToArray();
            if (!primaryKeyColumns.Any()) 
                return false;

            string alias = null;
            if (tableReference is TableReferenceWithAlias tableReferenceWithAlias)
                alias = tableReferenceWithAlias.Alias?.Value;

            KeyComparisonVisitor keyComparisonVisitor = new KeyComparisonVisitor(alias, primaryKeyColumns);
            whereClause.Accept(keyComparisonVisitor);
            return keyComparisonVisitor.Success;
        }

        private IEnumerable<string> DeterminePrimaryKeyColumns(TableReference tableReference)
        {
            switch (tableReference)
            {
                case NamedTableReference namedTable:
                {
                    Constraint primaryKey = base.Model.GetConstraints(namedTable.SchemaObject, throwOnError: false).SingleOrDefault(x => x.Kind == ConstraintKind.PrimaryKey);
                    if (primaryKey != null)
                        return primaryKey.Columns.Select(x => x.Name);

                    break;
                }

                case VariableTableReference variableTable:

                    if (this._tableVariablesWithKey.TryGetValue(variableTable.Variable.Name, out TableVariable tableVariable))
                        return tableVariable.PrimaryKeyColumns;

                    break;
            }

            return Enumerable.Empty<string>();
        }

        private class TableVariableWithKeyVisitor : TSqlFragmentVisitor
        {
            public ICollection<TableVariable> TableVariables { get; }

            public TableVariableWithKeyVisitor()
            {
                this.TableVariables = new Collection<TableVariable>();
            }

            public override void Visit(DeclareTableVariableBody node)
            {
                TableVariable tableVariable = new TableVariable(node.VariableName.Value);
                tableVariable.PrimaryKeyColumns.AddRange(GetPrimaryKeyColumns(node.Definition));
                this.TableVariables.Add(tableVariable);
            }

            private static IEnumerable<string> GetPrimaryKeyColumns(TableDefinition table)
            {
                // Look for global primary key constraint
                UniqueConstraintDefinition primaryKeyConstraint = table.TableConstraints.OfType<UniqueConstraintDefinition>().SingleOrDefault(x => x.IsPrimaryKey);
                if (primaryKeyConstraint != null)
                {
                    foreach (ColumnWithSortOrder column in primaryKeyConstraint.Columns)
                    {
                        yield return column.Column.GetName().Value;
                    }
                }

                // Look for a primary key column
                ColumnDefinition primaryKeyColumn = table.ColumnDefinitions.SingleOrDefault(x => x.Constraints.OfType<UniqueConstraintDefinition>().Any(y => y.IsPrimaryKey));
                if (primaryKeyColumn != null)
                    yield return primaryKeyColumn.ColumnIdentifier.Value;
            }
        }

        private class TableVariable
        {
            public string Name { get; }
            public ICollection<string> PrimaryKeyColumns { get; }

            public TableVariable(string name)
            {
                this.Name = name;
                this.PrimaryKeyColumns = new Collection<string>();
            }
        }

        private class KeyComparisonVisitor : TSqlFragmentVisitor
        {
            private readonly string _tableAlias;
            private readonly ICollection<string> _primaryKeyColumns;
            private bool _foundExists;

            public bool Success => !this._foundExists && !this._primaryKeyColumns.Any();

            public KeyComparisonVisitor(string tableAlias, IEnumerable<string> primaryKeyColumns)
            {
                this._tableAlias = tableAlias;
                this._primaryKeyColumns = new HashSet<string>(primaryKeyColumns);
            }

            public override void Visit(BooleanComparisonExpression node)
            {
                if (node.ComparisonType != BooleanComparisonType.Equals)
                    return;

                ScalarExpression[] expressions = { node.FirstExpression, node.SecondExpression };
                ColumnReferenceExpression columnReference = expressions.OfType<ColumnReferenceExpression>().FirstOrDefault();
                if (columnReference == null)
                    return;

                IList<Identifier> columnIdentifier = columnReference.MultiPartIdentifier.Identifiers;
                string tableAlias = columnIdentifier.Count > 1 ? columnIdentifier[0].Value : null;
                string columnName = columnIdentifier[columnIdentifier.Count > 1 ? 1 : 0].Value;

                if (this._tableAlias != tableAlias)
                    return;

                this._primaryKeyColumns.Remove(columnName);
            }

            public override void Visit(ExistsPredicate node)
            {
                this._foundExists = true;
            }
        }
    }
}