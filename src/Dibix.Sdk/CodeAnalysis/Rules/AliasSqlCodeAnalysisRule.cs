using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class AliasSqlCodeAnalysisRule : SqlCodeAnalysisRule<AliasSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 10;
        public override string ErrorMessage => "{0}";
    }

    public sealed class AliasSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(JoinTableReference node)
        {
            TableReferenceWithAlias unaliasedTable = new[] { node.FirstTableReference, node.SecondTableReference }.OfType<TableReferenceWithAlias>().FirstOrDefault(x => x.Alias == null);
            if (unaliasedTable != null)
                base.Fail(unaliasedTable, "Multiple table sources must be aliased");
        }
    }
}