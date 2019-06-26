using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    internal static class SqlParserExtensions
    {
        public static string Dump(this TSqlFragment fragment)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = fragment.FirstTokenIndex; i <= fragment.LastTokenIndex; i++)
                sb.Append(fragment.ScriptTokenStream[i].Text);

            return sb.ToString();
        }

        public static void Visit(this TSqlFragment fragment, Action<TSqlParserToken> visitor)
        {
            foreach (TSqlParserToken token in AsEnumerable(fragment))
                visitor(token);
        }

        public static IEnumerable<TSqlParserToken> AsEnumerable(this TSqlFragment fragment) => AsEnumerable(fragment, fragment.FirstTokenIndex);
        public static IEnumerable<TSqlParserToken> AsEnumerable(this TSqlFragment fragment, int startIndex)
        {
            for (int i = startIndex; i <= fragment.LastTokenIndex; i++)
                yield return fragment.ScriptTokenStream[i];
        }

        public static QuerySpecification FindQuerySpecification(this QueryExpression expression)
        {
            if (expression is QuerySpecification query)
                return query;

            UnionStatementVisitor visitor = new UnionStatementVisitor();
            expression.Accept(visitor);
            return visitor.FirstQueryExpression;
        }

        public static bool ContainsIf(this TSqlFragment fragment)
        {
            IfStatementVisitor visitor = new IfStatementVisitor();
            fragment.Accept(visitor);
            return visitor.Found;
        }

        public static IEnumerable<Constraint> CollectConstraints(this TableDefinition table)
        {
            foreach (ConstraintDefinition constraint in table.TableConstraints)
                yield return Constraint.Create(constraint);

            foreach (ColumnDefinition column in table.ColumnDefinitions)
            {
                foreach (ConstraintDefinition constraint in column.Constraints)
                    yield return Constraint.Create(constraint, new ColumnReference(column.ColumnIdentifier.Value, column));

                if (column.DefaultConstraint != null)
                    yield return Constraint.Create(column.DefaultConstraint, new ColumnReference(column.ColumnIdentifier.Value, column));
            }
        }

        public static bool IsNullable(this TableDefinition table, string columnName)
        {
            ICollection<Constraint> constraints = CollectConstraints(table).ToArray();
            bool notNullable = constraints.Where(x => x.Type == ConstraintType.Nullable && x.Columns[0].Name == columnName)
                                          .Select(x => x.Definition)
                                          .Cast<NullableConstraintDefinition>()
                                          .Any(x => !x.Nullable);
            if (notNullable)
                return false;

            bool isPartOfPk = constraints.Any(x => x.Type == ConstraintType.PrimaryKey && x.Columns.Any(y => y.Name == columnName));
            if (isPartOfPk)
                return false;

            return true;
        }

        public static IEnumerable<TableReference> Recursive(this IEnumerable<TableReference> references)
        {
            foreach (TableReference tableReference in references)
            {
                if (tableReference is QualifiedJoin join)
                {
                    yield return join.FirstTableReference;
                    yield return join.SecondTableReference;
                }
                else
                    yield return tableReference;
            }
        }

        public static bool IsTemporaryTable(this CreateTableStatement node) => node.SchemaObjectName.BaseIdentifier.Value[0] == '#';

        private class IfStatementVisitor : TSqlFragmentVisitor
        {
            public bool Found { get; private set; }

            public override void ExplicitVisit(IfStatement node) => this.Found = true;
        }

        private class UnionStatementVisitor : TSqlFragmentVisitor
        {
            public QuerySpecification FirstQueryExpression { get; private set; }

            public override void Visit(QuerySpecification node)
            {
                if (this.FirstQueryExpression != null)
                    return;

                this.FirstQueryExpression = node;
            }
        }
    }
}
