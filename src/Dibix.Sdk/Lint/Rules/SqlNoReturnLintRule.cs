using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    public sealed class SqlNoReturnLintRule : SqlLintRule
    {
        public override int Id { get { return 3; } }
        public override string ErrorMessage { get { return "The use of RETURN expressions is not allowed"; } }

        public override void ExplicitVisit(ReturnStatement fragment)
        {
            base.Fail(fragment.ScriptTokenStream[fragment.FirstTokenIndex]);
        }
    }
}