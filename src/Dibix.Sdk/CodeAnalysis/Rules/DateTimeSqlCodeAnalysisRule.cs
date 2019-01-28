using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    // Disabled, since it's more of a labs rule and not stable enough
    public sealed class DateTimeSqlCodeAnalysisRule : SqlCodeAnalysisRule<DateTimeSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 28;
        public override string ErrorMessage => "{0}";
        public override bool IsEnabled => false;
    }

    public sealed class DateTimeSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(StringLiteral node)
        {
            if (Regex.IsMatch(node.Value, @"^((\d\d-\d\d-\d\d\d\d)|(\d\d\d\d-\d\d-\d\d)|(\d\d\/\d\d\/\d\d\d\d)|(\d\d\d\d\/\d\d\/\d\d))"))
                base.Fail(node, $"Possible datetime constant: {node.Value}");
        }

        public override void Visit(FunctionCall function)
        {
            if (function.FunctionName.Value.ToUpper() == "GETDATE")
                base.Fail(function, $"Use of non UTC date/time function: {function.FunctionName.Value}");
        }
    }
}