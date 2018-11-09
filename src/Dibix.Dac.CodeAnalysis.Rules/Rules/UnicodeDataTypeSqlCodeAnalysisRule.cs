using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Dac.CodeAnalysis.Rules
{
    public sealed class UnicodeDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule<UnicodeDataTypeSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 5;
        public override string ErrorMessage => "Use unicode data types instead of ascii. Please replace VARCHAR with NVARCHAR.";
    }

    public sealed class UnicodeDataTypeSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(SqlDataTypeReference node)
        {
            if (node.SqlDataTypeOption == SqlDataTypeOption.VarChar)
                base.Fail(node);
        }
    }
}