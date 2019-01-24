using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class UniqueIndexSqlCodeAnalysisRule : SqlCodeAnalysisRule<UniqueIndexSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 27;
        public override string ErrorMessage => "Unique index should be replaced by a unique constraint on the table definition: {0}";
    }

    public sealed class UniqueIndexSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(CreateIndexStatement node)
        {
            if (node.Unique && node.FilterPredicate == null && !node.IncludeColumns.Any())
                base.Fail(node, node.Name.Value);
        }
    }
}
 