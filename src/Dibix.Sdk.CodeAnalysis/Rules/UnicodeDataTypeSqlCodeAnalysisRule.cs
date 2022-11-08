using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 5)]
    public sealed class UnicodeDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Use unicode data types instead of ascii. Replace '{0}' with '{1}'.";

        public UnicodeDataTypeSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(SqlDataTypeReference node)
        {
            if (node.SqlDataTypeOption == SqlDataTypeOption.Char || node.SqlDataTypeOption == SqlDataTypeOption.VarChar)
                base.Fail(node, node.SqlDataTypeOption.ToString().ToUpperInvariant(), $"N{node.SqlDataTypeOption}".ToUpperInvariant());
        }
    }
}