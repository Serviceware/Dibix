using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 10)]
    public sealed class AliasSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "{0}";

        public AliasSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(JoinTableReference node)
        {
            TableReferenceWithAlias unaliasedTable = EnumerableExtensions.Create(node.FirstTableReference, node.SecondTableReference)
                                                                         .OfType<TableReferenceWithAlias>()
                                                                         .FirstOrDefault(x => x.Alias == null);

            if (unaliasedTable != null)
                base.Fail(unaliasedTable, "Multiple table sources must be aliased");
        }
    }
}