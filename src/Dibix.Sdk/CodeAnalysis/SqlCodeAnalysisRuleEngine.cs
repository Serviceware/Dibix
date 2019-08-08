using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisRuleEngine : ISqlCodeAnalysisRuleEngine
    {
        #region Fields
        private readonly ICollection<ISqlCodeAnalysisRule> _rules;
        #endregion

        #region Constructor
        public SqlCodeAnalysisRuleEngine()
        {
            this._rules = ScanRules();
        }
        #endregion

        #region Public Methods
        public IEnumerable<SqlCodeAnalysisError> Analyze(TSqlObject modelElement, TSqlFragment scriptFragment)
        {
            return this._rules.SelectMany(x => x.Analyze(modelElement, scriptFragment));
        }

        public IEnumerable<SqlCodeAnalysisError> Analyze(ISqlCodeAnalysisRule rule, string scriptFilePath)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(scriptFilePath);
            return rule.Analyze(null, fragment);
        }
        #endregion

        #region Private Methods
        private static ICollection<ISqlCodeAnalysisRule> ScanRules()
        {
            Type ruleDefinitionType = typeof(ISqlCodeAnalysisRule);
            IEnumerable<ISqlCodeAnalysisRule> rules = ruleDefinitionType.Assembly
                                                                        .GetLoadableTypes()
                                                                        .Where(x => ruleDefinitionType.IsAssignableFrom(x) && x.IsClass && !x.IsAbstract)
                                                                        .Select(type => (ISqlCodeAnalysisRule)Activator.CreateInstance(type))
                                                                        .Where(x => x.IsEnabled);

            IDictionary<int, ISqlCodeAnalysisRule> ruleMap = new Dictionary<int, ISqlCodeAnalysisRule>();
            foreach (ISqlCodeAnalysisRule rule in rules)
            {
                if (ruleMap.TryGetValue(rule.Id, out ISqlCodeAnalysisRule conflictingRule))
                    throw new InvalidOperationException($"The rule '{conflictingRule}' is already registered for id '{rule.Id}'");

                ruleMap.Add(rule.Id, rule);
            }
            return ruleMap.Values;
        }
        #endregion
    }
}