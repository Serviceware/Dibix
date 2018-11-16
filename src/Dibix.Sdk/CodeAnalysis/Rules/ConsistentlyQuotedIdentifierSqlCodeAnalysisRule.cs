using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class ConsistentlyQuotedIdentifierSqlCodeAnalysisRule : SqlCodeAnalysisRule<ConsistentlyQuotedIdentifierSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 11;
        public override string ErrorMessage => "Identifier quotation should be consistent and not mixed. Either use all square brackets or none: {0}";
    }

    public sealed class ConsistentlyQuotedIdentifierSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(MultiPartIdentifier node)
        {
            bool allEqual = node.Identifiers
                                .Where(x => !SqlConstants.ReservedFunctionNames.Contains(x.Value))
                                .Select(x => x.QuoteType)
                                .Distinct()
                                .Count() > 1;
            if (allEqual)
                base.Fail(node, node.Dump());
        }
    }
}