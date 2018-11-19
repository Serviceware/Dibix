using System.Collections.Generic;
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
        private static readonly HashSet<string> SpecialAliases = new HashSet<string>
        {
            "S" // Source in MERGE
          , "T" // Target in Merge
        };

        public override void Visit(MultiPartIdentifier node)
        {
            IList<Identifier> identifiers = node.Identifiers
                                                .Where(x => !SqlConstants.ReservedFunctionNames.Contains(x.Value))
                                                .ToArray();

            // S.[x] or T.[x]
            if (identifiers.Count == 2 && SpecialAliases.Contains(identifiers[0].Value))
                return;

            bool allEqual = identifiers.Select(x => x.QuoteType)
                                       .Distinct()
                                       .Count() > 1;
            if (allEqual)
                base.Fail(node, node.Dump());
        }
    }
}