using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class UnfilteredDataModificationSqlCodeAnalysisRule : SqlCodeAnalysisRule<UnfilteredDataModificationSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 18;
        public override string ErrorMessage => "Missing where clause in {0} statement";
    }

    public sealed class UnfilteredDataModificationSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(UpdateDeleteSpecificationBase node)
        {
            if (node.WhereClause != null)
                return;

            string specificationName = node.GetType().Name;
            int specificationIndex = specificationName.IndexOf("Specification", StringComparison.Ordinal);
            string typeName = (specificationIndex >= 0 ? specificationName.Substring(0, specificationIndex) : specificationName).ToLowerInvariant();
            base.Fail(node, typeName);
        }
    }
}