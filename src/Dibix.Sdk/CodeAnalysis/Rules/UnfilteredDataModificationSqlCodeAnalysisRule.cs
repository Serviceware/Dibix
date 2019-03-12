using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class UnfilteredDataModificationSqlCodeAnalysisRule : SqlCodeAnalysisRule<UnfilteredDataModificationSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 18;
        public override string ErrorMessage => "Missing where clause in {0} statement";
    }

    public sealed class UnfilteredDataModificationSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(UpdateSpecification node)
        {
            if (node.WhereClause != null)
                return;

            // Table variables are allowed for update
            if (IsTableVariableReference(node))
                return;

            base.Fail(node, "UPDATE");
        }

        public override void Visit(DeleteSpecification node)
        {
            if (node.WhereClause != null)
                return;

            // Table variables are allowed for delete
            if (IsTableVariableReference(node))
                return;

            // We don't investigate complex join filter logic and concentrate on simple DELETE FROM x WHERE
            if (node.FromClause != null && node.FromClause.TableReferences.OfType<QualifiedJoin>().Any())
                return;

            base.Fail(node, "DELETE");
        }

        private static bool IsTableVariableReference(UpdateDeleteSpecificationBase node)
        {
            // UPDATE @x
            if (node.Target is VariableTableReference)
                return true;

            // UPDATE [x] .. FROM @x AS [x]
            if (!(node.Target is NamedTableReference tableName)) // UPDATE [x]
                return false;

            if (node.FromClause == null)
                return false;

            if (node.FromClause
                    .TableReferences
                    .Recursive()
                    .OfType<TableReferenceWithAlias>() // FROM @x AS [x]
                    .Any(x => x.Alias.Value /* FROM @x AS [x] */ == tableName.SchemaObject.BaseIdentifier.Value /* UPDATE [x] */))
                return true;

            return false;
        }
    }
}