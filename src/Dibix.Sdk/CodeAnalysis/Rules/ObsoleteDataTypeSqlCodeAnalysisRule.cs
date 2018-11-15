using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class ObsoleteDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule<ObsoleteDataTypeSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 16;
        public override string ErrorMessage => "The data type '{0}' is obsolete and should not be used";
    }

    public sealed class ObsoleteDataTypeSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private static readonly HashSet<SqlDataTypeOption> ObsoleteDataTypes = new HashSet<SqlDataTypeOption>
        {
            SqlDataTypeOption.NText,
            SqlDataTypeOption.Image
        };

        public override void Visit(SqlDataTypeReference node)
        {
            if (ObsoleteDataTypes.Contains(node.SqlDataTypeOption))
                base.Fail(node, node.SqlDataTypeOption.ToString().ToUpperInvariant());
        }
    }
}