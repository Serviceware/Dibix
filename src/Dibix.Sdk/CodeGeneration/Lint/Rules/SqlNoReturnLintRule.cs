using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration.Lint.Rules
{
    public sealed class SqlNoReturnLintRule : SqlLintRule
    {
        public override int Id => 3;
        public override string ErrorMessage => "The use of RETURN expressions is not allowed";

        public override void ExplicitVisit(ReturnStatement fragment)
        {
            base.Fail(fragment);
        }
    }
}