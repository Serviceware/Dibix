using System.Collections.Generic;
using Microsoft.SqlServer.Dac.CodeAnalysis;

namespace Dibix.Dac.CodeAnalysis.Rules
{
    public interface ISqlCodeAnalysisRule
    {
        IEnumerable<SqlRuleProblem> Analyze(SqlRuleExecutionContext context);
    }
}