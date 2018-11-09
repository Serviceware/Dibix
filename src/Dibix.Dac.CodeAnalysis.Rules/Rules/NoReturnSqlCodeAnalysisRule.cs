using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Dac.CodeAnalysis.Rules
{
    public sealed class NoReturnSqlCodeAnalysisRule : SqlCodeAnalysisRule<NoReturnSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 3;
        public override string ErrorMessage => "The use of RETURN expressions is not allowed";
    }

    public sealed class NoReturnSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void ExplicitVisit(ReturnStatement fragment)
        {
            base.Fail(fragment);
        }
    }
}