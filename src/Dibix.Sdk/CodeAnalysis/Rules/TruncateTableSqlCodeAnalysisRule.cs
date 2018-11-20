using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class TruncateTableSqlCodeAnalysisRule : SqlCodeAnalysisRule<TruncateTableSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 19;
        public override string ErrorMessage => "Found use of TRUNCATE TABLE statement";
    }

    public sealed class TruncateTableSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(TruncateTableStatement node) => base.Fail(node);
    }
}