using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dibix.Sdk.CodeAnalysis
{
    internal static class SqlCodeAnalysisRuleMap
    {
        private static readonly IDictionary<Type, RuleRegistration> RuleMap = ScanRules().ToDictionary(x => x.Type);

        public static IReadOnlyCollection<Type> AllRules { get; } = RuleMap.Values.Where(x => x.IsEnabled).Select(x => x.Type).ToArray();
        public static IReadOnlyCollection<Type> EnabledRules { get; } = RuleMap.Values.Where(x => x.IsEnabled).Select(x => x.Type).ToArray();

        public static int GetRuleId(Type type) => RuleMap[type].Id;

        public static Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>> GetHandler(Type type)
        {
            if (!RuleMap.TryGetValue(type, out RuleRegistration registration))
                throw new InvalidOperationException($"SqlCodeAnalysisRule not registered: {type}");

            return registration.Handler;
        }

        private static IEnumerable<RuleRegistration> ScanRules()
        {
            IDictionary<int, Type> ruleMap = new Dictionary<int, Type>();
            foreach (Type type in typeof(SqlCodeAnalysisRuleAttribute).Assembly.GetLoadableTypes())
            {
                SqlCodeAnalysisRuleAttribute attribute = type.GetCustomAttribute<SqlCodeAnalysisRuleAttribute>();
                if (attribute == null)
                    continue;

                if (ruleMap.TryGetValue(attribute.Id, out Type conflictingRule))
                    throw new InvalidOperationException($"The rule '{conflictingRule}' is already registered for id '{attribute.Id}'");

                ruleMap.Add(attribute.Id, type);
                yield return new RuleRegistration(attribute.Id, type, attribute.IsEnabled, CompileRuleInvoker(type));
            }
        }

        private static Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>> CompileRuleInvoker(Type ruleType)
        {
            ParameterExpression contextParameter = Expression.Parameter(typeof(SqlCodeAnalysisContext), "context");
            ConstructorInfo ctor = ruleType.GetConstructorSafe(typeof(SqlCodeAnalysisContext));
            Expression ruleInstance = Expression.New(ctor, contextParameter);
            MethodInfo analyzeMethod = typeof(ISqlCodeAnalysisRule).SafeGetMethod(nameof(ISqlCodeAnalysisRule.Analyze));
            Expression fragment = Expression.Property(contextParameter, nameof(SqlCodeAnalysisContext.Fragment));
            Expression analyzeCall = Expression.Call(ruleInstance, analyzeMethod, fragment);
            Expression<Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>>> lambda = Expression.Lambda<Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>>>(analyzeCall, contextParameter);
            Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>> compiled = lambda.Compile();
            return compiled;
        }

        private struct RuleRegistration
        {
            public int Id { get; }
            public Type Type { get; }
            public bool IsEnabled { get; }
            public Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>> Handler { get; }

            public RuleRegistration(int id, Type type, bool isEnabled, Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>> handler)
            {
                Id = id;
                Type = type;
                IsEnabled = isEnabled;
                Handler = handler;
            }
        }
    }
}