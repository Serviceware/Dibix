using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeAnalysis;
using Microsoft.SqlServer.Dac.CodeAnalysis;

namespace Dibix.Sdk.Tests.CodeAnalysis
{
    [ExportCodeAnalysisRule(RuleId, RuleId, RuleScope = SqlRuleScope.Element)]
    public sealed class SqlCodeAnalysisRuleDecorator : SqlCodeAnalysisRule
    {
        public const string RuleId = "Dibix.Sdk.Tests.CodeAnalysis";

        public ISqlCodeAnalysisRule Rule { get; set; }

        public SqlCodeAnalysisRuleDecorator() => this.SupportedElementTypes = SqlElementType.Types;

        public override IList<SqlRuleProblem> Analyze(SqlRuleExecutionContext ruleExecutionContext) => this.Rule.Analyze(ruleExecutionContext).ToArray();
    }
}