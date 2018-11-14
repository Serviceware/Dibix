using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    // This rule is disabled since it's not stable enough and its use is not yet clear
    /*
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
    */
}