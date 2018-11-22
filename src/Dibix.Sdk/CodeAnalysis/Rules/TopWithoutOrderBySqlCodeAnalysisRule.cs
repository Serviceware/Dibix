using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    // This rule is disabled since it's too heavy, because there are many use cases,
    // where it is clear that one row will be returned. i.E.: GetById/SingleOrDefault
    /*
    public sealed class TopWithoutOrderBySqlCodeAnalysisRule : SqlCodeAnalysisRule<TopWithoutOrderBySqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 22;
        public override string ErrorMessage => "Missing ORDER BY for SELECT TOP statement";
    }

    public sealed class TopWithoutOrderBySqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(QuerySpecification node)
        {
            if (node.TopRowFilter != null && node.OrderByClause == null)
                base.Fail(node);
        }
    }
    */
}