using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 18)]
    public sealed class UnfilteredDataModificationSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Missing where clause in {0} statement";

        public UnfilteredDataModificationSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(UpdateSpecification node)
        {
            if (node.WhereClause != null)
                return;

            if (IsValid(node))
                return;

            base.Fail(node, "UPDATE");
        }

        public override void Visit(DeleteSpecification node)
        {
            if (node.WhereClause != null)
                return;

            if (IsValid(node))
                return;

            // We don't investigate complex join filter logic and concentrate on simple DELETE FROM x WHERE
            if (node.FromClause != null && node.FromClause.TableReferences.OfType<QualifiedJoin>().Any())
                return;

            base.Fail(node, "DELETE");
        }

        private static bool IsValid(UpdateDeleteSpecificationBase node)
        {
            // UPDATE @x
            bool isTableVariableTarget = node.Target is VariableTableReference;
            if (isTableVariableTarget)
                return true;

            // UPDATE [dbo].[table] or UPDATE [alias]
            if (!(node.Target is NamedTableReference tableName))
                return false;

            if (node.FromClause == null) // ?
                return false;

            // UPDATE [x] .. FROM [dbo].[table] AS [x]
            bool isAliasedTarget = node.FromClause
                                       .TableReferences
                                       .Recursive()
                                       .OfType<TableReferenceWithAlias>() // FROM @x AS [x]
                                       .Any(x => x.Alias.Value /* FROM @x AS [x] */ == tableName.SchemaObject.BaseIdentifier.Value /* UPDATE [x] */);

            if (isAliasedTarget)
                return true;

            return false;
        }
    }
}