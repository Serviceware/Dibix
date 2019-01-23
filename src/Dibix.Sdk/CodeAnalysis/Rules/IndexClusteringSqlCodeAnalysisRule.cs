using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class IndexClusteringSqlCodeAnalysisRule : SqlCodeAnalysisRule<IndexClusteringSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 25;
        public override string ErrorMessage => "Please specify the clustering (CLUSTERED/NONCLUSTERED) for the index '{0}' and don't rely on the default";
    }

    public sealed class IndexClusteringSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(CreateIndexStatement node)
        {
            if (!node.Clustered.HasValue)
                base.Fail(node, node.Name.Value);
        }
    }
}
 