using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeAnalysis;
using Microsoft.SqlServer.Dac.CodeAnalysis;

namespace Dibix.Sdk.Dac
{
    internal sealed class DacSqlCodeAnalysisAdapter
    {
        private readonly ISqlCodeAnalysisRuleEngine _engine;

        public DacSqlCodeAnalysisAdapter()
        {
            this._engine = new SqlCodeAnalysisRuleEngine();
        }

        public IList<SqlRuleProblem> Analyze(SqlRuleExecutionContext context)
        {
            return this._engine
                       .Analyze(context.ModelElement, context.ScriptFragment)
                       .Select(x => x.ToProblem())
                       .ToArray();
        }
    }
}