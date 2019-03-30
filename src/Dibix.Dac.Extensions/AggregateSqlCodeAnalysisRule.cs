using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dibix.Sdk;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Model;
using Assembly = System.Reflection.Assembly;

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
            this.SupportedElementTypes = ModelSchema.SchemaInstance.TopLevelTypes.ToArray();
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
            string dacDirectory = Path.GetDirectoryName(typeof(SqlCodeAnalysisRule).Assembly.Location);

            Assembly OnAssemblyResolve(object sender, ResolveEventArgs e)
            {
                string relatedAssemblyPath = Path.Combine(dacDirectory, String.Concat(new AssemblyName(e.Name).Name, ".dll"));
                return File.Exists(relatedAssemblyPath) ? Assembly.LoadFrom(relatedAssemblyPath) : null;
            }

            Assembly rulesAssembly = SdkAssemblyLoader.LocatePackageRootAndLoad(source.SourceName);
            Type providerType = rulesAssembly.GetType("Dibix.Sdk.Dac.DacSqlCodeAnalysisAdapter");
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            object engine = Activator.CreateInstance(providerType);
            Expression instance = Expression.Constant(engine);
            ParameterExpression parameter = Expression.Parameter(typeof(SqlRuleExecutionContext), "context");
            Expression call = Expression.Call(instance, "Analyze", new Type[0], parameter);
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            return Expression.Lambda<Func<SqlRuleExecutionContext, IEnumerable<SqlRuleProblem>>>(call, parameter).Compile();
        }

        private static void AttachDebugger()
        {
            if (File.Exists(String.Concat(typeof(AggregateSqlCodeAnalysisRule).Assembly.Location, ".lock")))
                Debugger.Launch();
        }
    }
}