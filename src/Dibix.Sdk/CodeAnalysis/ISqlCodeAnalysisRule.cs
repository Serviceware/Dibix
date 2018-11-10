using System.Collections.Generic;
using Microsoft.SqlServer.Dac.CodeAnalysis;

namespace Dibix.Sdk.CodeAnalysis
{
    public interface ISqlCodeAnalysisRule
    {
        IEnumerable<SqlRuleProblem> Analyze(SqlRuleExecutionContext context);
    }
}