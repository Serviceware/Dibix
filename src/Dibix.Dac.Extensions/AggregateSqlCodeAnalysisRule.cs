using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.CodeAnalysis;

namespace Dibix.Dac.Extensions
{
    [ExportCodeAnalysisRule(
        id: "Dibix.SRDBX",
        displayName: "All rules",
        Category = "Rules",
        Description = "All dibix rules",
        RuleScope = SqlRuleScope.Element)]
    public sealed class AggregateSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private static Func<SqlRuleExecutionContext, IEnumerable<SqlRuleProblem>> _analyzer;

        static AggregateSqlCodeAnalysisRule()
        {
            AttachDebugger();
        }

        public AggregateSqlCodeAnalysisRule()
        {
            this.SupportedElementTypes = SqlElementType.Types;
        }

        public override IList<SqlRuleProblem> Analyze(SqlRuleExecutionContext ruleExecutionContext)
        {
            SourceInformation source = ruleExecutionContext.ModelElement.GetSourceInformation();
            if (source == null)
                return new SqlRuleProblem[0];

            EnsureAnalyzer(source);
            return _analyzer(ruleExecutionContext).ToArray();
        }

        private static void EnsureAnalyzer(SourceInformation source)
        {
            if (_analyzer == null)
                _analyzer = BuildAnalyzer(source);
        }

        private static Func<SqlRuleExecutionContext, IEnumerable<SqlRuleProblem>> BuildAnalyzer(SourceInformation source)
        {
            string sourcePath = DependentAssemblyLocator.LocateRulesAssembly(source.SourceName);
            string targetPath = Path.GetTempFileName();
            File.Copy(sourcePath, targetPath, true);
            string dacDirectory = Path.GetDirectoryName(typeof(SqlCodeAnalysisRule).Assembly.Location);

            Assembly OnAssemblyResolve(object sender, ResolveEventArgs e)
            {
                string relatedAssemblyPath = Path.Combine(dacDirectory, String.Concat(new AssemblyName(e.Name).Name, ".dll"));
                if (File.Exists(relatedAssemblyPath)) return Assembly.LoadFrom(relatedAssemblyPath);

                return null;
            }

            Assembly assembly = Assembly.LoadFrom(targetPath);
            Type providerType = assembly.GetType("Dibix.Sdk.CodeAnalysis.Dac.DacSqlCodeAnalysisAdapter");
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            object engine = Activator.CreateInstance(providerType);
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            Expression instance = Expression.Constant(engine);
            ParameterExpression parameter = Expression.Parameter(typeof(SqlRuleExecutionContext), "context");
            Expression call = Expression.Call(instance, "Analyze", new Type[0], parameter);
            return Expression.Lambda<Func<SqlRuleExecutionContext, IEnumerable<SqlRuleProblem>>>(call, parameter).Compile();
        }

        private static void AttachDebugger()
        {
            if (File.Exists(String.Concat(typeof(AggregateSqlCodeAnalysisRule).Assembly.Location, ".lock")))
                Debugger.Launch();
        }
    }
}