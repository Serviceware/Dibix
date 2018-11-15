using System.Linq;
using Dibix.Sdk.CodeGeneration;
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
        public override void Visit(SchemaObjectName name)
        {
            if (name.Identifiers.Select(x => x.QuoteType).Distinct().Count() > 1)
                base.Fail(name, name.Dump());
        }
    }
}