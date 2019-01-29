using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class DateTimeSqlCodeAnalysisRule : SqlCodeAnalysisRule<DateTimeSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 28;
        public override string ErrorMessage => "{0}";
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