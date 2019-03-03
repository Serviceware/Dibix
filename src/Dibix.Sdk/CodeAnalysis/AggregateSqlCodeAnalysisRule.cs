using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeAnalysis
{
    [ExportCodeAnalysisRule(
        id: "Dibix.SRDBX",
        displayName: "All rules",
        Category = "Rules",
        Description = "All dibix rules",
        RuleScope = SqlRuleScope.Element)]
    public sealed class AggregateSqlCodeAnalysisRule : Microsoft.SqlServer.Dac.CodeAnalysis.SqlCodeAnalysisRule
    {
        private static Func<SqlRuleExecutionContext, IEnumerable<SqlRuleProblem>> _analyzer;

        public AggregateSqlCodeAnalysisRule()
        {
            this.SupportedElementTypes = ModelSchema.SchemaInstance.TopLevelTypes.ToArray();
        }

        public override IList<SqlRuleProblem> Analyze(SqlRuleExecutionContext ruleExecutionContext)
        {
            return new SqlRuleProblem[0];
        }
    }
}
