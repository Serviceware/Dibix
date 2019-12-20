using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class MisusedTopRowFilterSqlCodeAnalysisRule : SqlCodeAnalysisRule<MisusedTopRowFilterSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 22;
        public override string ErrorMessage => "{0}";
    }

    public sealed class MisusedTopRowFilterSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(QuerySpecification node)
        {
            if (node.TopRowFilter == null) 
                return;

            if (node.WhereClause != null
             && node.FromClause != null
             && node.FromClause.TableReferences.Count == 1
             && node.FromClause.TableReferences[0] is NamedTableReference namedTable)
            {
                string tableAlias = namedTable.Alias?.Value;
                Constraint primaryKey = base.Model.GetConstraints(namedTable.SchemaObject, throwOnError: false).SingleOrDefault(x => x.Kind == ConstraintKind.PrimaryKey);
                if (primaryKey != null)
                {
                    KeyComparisonVisitor keyComparisonVisitor = new KeyComparisonVisitor(tableAlias, primaryKey.Columns.Select(x => x.Name).ToArray());
                    node.WhereClause.Accept(keyComparisonVisitor);
                    if (keyComparisonVisitor.Success)
                    {
                        base.Fail(node, "Invalid TOP filter for single row equality statement");
                        return;
                    }
                }
            }

            if (node.OrderByClause == null) 
                base.Fail(node.TopRowFilter, "Missing ORDER BY for SELECT TOP statement");
        }

        private class KeyComparisonVisitor : TSqlFragmentVisitor
        {
            private readonly string _tableAlias;
            private readonly HashSet<string> _primaryKeyColumns;
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