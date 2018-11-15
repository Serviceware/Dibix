using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class MultiTableAliasSqlCodeAnalysisRule : SqlCodeAnalysisRule<MultiTableAliasSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 10;
        public override string ErrorMessage => "Unaliased table found in multi table joins";
    }

    public sealed class MultiTableAliasSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(JoinTableReference join)
        {
            if (new[] { join.FirstTableReference, join.SecondTableReference }.OfType<TableReferenceWithAlias>().Any(x => x.Alias == null))
                base.Fail(join);
        }
    }
}