using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dibix.Dac.CodeAnalysis.Rules;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.CodeAnalysis;

namespace Dibix.Dac.CodeAnalysis
{
    [ExportCodeAnalysisRule(
        id: "Dibix.SRDBX",
        displayName: "All rules",
        Category = "Rules",
        Description = "All dibix rules",
        RuleScope = SqlRuleScope.Element)]
    public sealed class AggregateSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private static ISqlCodeAnalysisRuleEngine _engine;

        public AggregateSqlCodeAnalysisRule()
        {
            System.Diagnostics.Debugger.Launch();
            this.SupportedElementTypes = SqlElementType.Types;
        }

        public override IList<SqlRuleProblem> Analyze(SqlRuleExecutionContext ruleExecutionContext)
        {
            SourceInformation source = ruleExecutionContext.ModelElement.GetSourceInformation();
            if (source == null)
                return new SqlRuleProblem[0];

            EnsureEngine(source);
            return _engine.Analyze(ruleExecutionContext).ToArray();
        }

        private static void EnsureEngine(SourceInformation source)
        {
            if (_engine == null)
                _engine = CreateEngine(source);
        }

        private static ISqlCodeAnalysisRuleEngine CreateEngine(SourceInformation source)
        {
            string sourcePath = RulesAssemblyLocator.Locate(source.SourceName);
            string targetPath = Path.GetTempFileName();
            File.Copy(sourcePath, targetPath, true);
            Assembly assembly = Assembly.LoadFrom(targetPath);
            Type providerType = assembly.GetType("Dibix.Dac.CodeAnalysis.Rules.SqlCodeAnalysisRuleEngine");
            return (ISqlCodeAnalysisRuleEngine)Activator.CreateInstance(providerType);
        }
    }
}
