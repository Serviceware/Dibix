using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    // This rule is disabled since it's not stable enough and its use is not yet clear
    [SqlCodeAnalysisRule(id: 11, IsEnabled = false)]
    public sealed class ConsistentlyQuotedIdentifierSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Identifier quotation should be consistent and not mixed. Either use all square brackets or none: {0}";

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
 