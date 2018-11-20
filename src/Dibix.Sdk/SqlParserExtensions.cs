using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
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

        public static IEnumerable<TSqlParserToken> AsEnumerable(this TSqlFragment fragment)
        {
            for (int i = fragment.FirstTokenIndex; i <= fragment.LastTokenIndex; i++)
                yield return fragment.ScriptTokenStream[i];
        }

        public static bool ContainsIf(this TSqlFragment fragment)
        {
            IfStatementVisitor visitor = new IfStatementVisitor();
            fragment.Accept(visitor);
            return visitor.Found;
        }

        public static IEnumerable<Constraint> CollectConstraints(this TableDefinition table, ConstraintScope filter = ConstraintScope.All)
        {
            if (filter.HasFlag(ConstraintScope.Table))
            {
                foreach (ConstraintDefinition constraint in table.TableConstraints)
                    yield return Constraint.Create(constraint);
            }

            if (filter.HasFlag(ConstraintScope.Column))
            {
                foreach (ColumnDefinition column in table.ColumnDefinitions)
                {
                    foreach (ConstraintDefinition constraint in column.Constraints)
                        yield return Constraint.Create(constraint, column.ColumnIdentifier.Value);

                    if (column.DefaultConstraint != null)
                        yield return Constraint.Create(column.DefaultConstraint, column.ColumnIdentifier.Value);
                }
            }
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

        private class IfStatementVisitor : TSqlFragmentVisitor
        {
            public bool Found { get; private set; }

            public override void ExplicitVisit(IfStatement node)
            {
                this.Found = true;
            }
        }
    }
}
