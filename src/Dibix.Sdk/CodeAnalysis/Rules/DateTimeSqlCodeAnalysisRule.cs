using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 28)]
    public sealed class DateTimeSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "{0}";

        public override void Visit(FunctionCall function)
        {
            if (function.FunctionName.Value.ToUpper() == "GETDATE")
                base.Fail(function, $"Use of non UTC date/time function: {function.FunctionName.Value}");
        }
    }
}