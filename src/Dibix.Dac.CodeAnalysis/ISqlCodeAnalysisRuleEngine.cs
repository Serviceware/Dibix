using System.Collections.Generic;
using Microsoft.SqlServer.Dac.CodeAnalysis;

namespace Dibix.Dac.CodeAnalysis
{
    public interface ISqlCodeAnalysisRuleEngine
    {
        IEnumerable<SqlRuleProblem> Analyze(SqlRuleExecutionContext context);
    }
}