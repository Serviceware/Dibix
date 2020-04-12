using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.CodeAnalysis
{
    internal static class SqlCodeAnalysisRuleMap
    {
        private static readonly IDictionary<Type, SqlCodeAnalysisRuleAttribute> Rules = ScanRules().ToDictionary(x => x.Key, x => x.Value);

        public static IEnumerable<Type> EnabledRules => Rules.Where(x => x.Value.IsEnabled).Select(x => x.Key);

        public static int GetRuleId(Type type) => Rules[type].Id;

        private static IEnumerable<KeyValuePair<Type, SqlCodeAnalysisRuleAttribute>> ScanRules()
        {
            IDictionary<int, Type> ruleMap = new Dictionary<int, Type>();
            foreach (Type type in typeof(SqlCodeAnalysisRuleAttribute).Assembly.GetLoadableTypes())
            {
                SqlCodeAnalysisRuleAttribute attrib = type.GetCustomAttribute<SqlCodeAnalysisRuleAttribute>();
                if (attrib == null)
                    continue;

                if (ruleMap.TryGetValue(attrib.Id, out Type conflictingRule))
                    throw new InvalidOperationException($"The rule '{conflictingRule}' is already registered for id '{attrib.Id}'");

                ruleMap.Add(attrib.Id, type);
                yield return new KeyValuePair<Type, SqlCodeAnalysisRuleAttribute>(type, attrib);
            }
        }
    }
}