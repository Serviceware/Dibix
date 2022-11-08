using System;
using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 41)]
    public sealed class InvalidFunctionUsageSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private static readonly IDictionary<string, int> ParameterCountMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["ISNULL"] = 2
        };

        protected override string ErrorMessageTemplate => "{0}";

        public InvalidFunctionUsageSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(FunctionCall node)
        {
            string functionName = node.FunctionName.Value;
            if (!ParameterCountMap.TryGetValue(functionName, out int parameterCount)) 
                return;

            if (node.Parameters.Count != parameterCount)
                base.Fail(node, $"The {functionName} function requires {parameterCount} argument(s).");
        }
    }
}