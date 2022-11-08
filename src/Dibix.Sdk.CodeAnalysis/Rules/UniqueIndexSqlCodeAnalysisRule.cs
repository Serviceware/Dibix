using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 27)]
    public sealed class UniqueIndexSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Unique index should be replaced by a unique constraint on the table definition: {0}";

        public UniqueIndexSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(CreateIndexStatement node)
        {
            if (node.Unique && node.FilterPredicate == null && !node.IncludeColumns.Any())
                base.Fail(node, node.Name.Value);
        }
    }
}
 