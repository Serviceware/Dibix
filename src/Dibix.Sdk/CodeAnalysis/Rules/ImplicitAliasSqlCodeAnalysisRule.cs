using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class ImplicitAliasSqlCodeAnalysisRule : SqlCodeAnalysisRule<ImplicitAliasSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 8;
        public override string ErrorMessage => "Aliases must be marked with 'AS'";
    }

    public sealed class ImplicitAliasSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(TableReferenceWithAlias node)
        {
            if (node.Alias == null)
                return;

            for (int i = node.Alias.FirstTokenIndex; i > node.FirstTokenIndex; i--)
            {
                if (node.ScriptTokenStream[i].TokenType == TSqlTokenType.As)
                    return;
            }
            base.Fail(node.Alias);
        }
    }
}