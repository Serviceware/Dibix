using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            using (TextReader reader = new StreamReader(scriptFilePath))
            {
                TSqlParser parser = new TSql140Parser(true);
                IList<ParseError> parseErrors;
                TSqlFragment fragment = parser.Parse(reader, out parseErrors);
                return rule.Analyze(null, fragment);
            }
        }
        #endregion

        #region Private Methods
        private static ICollection<ISqlCodeAnalysisRule> ScanRules()
        {
            Type ruleDefinitionType = typeof(ISqlCodeAnalysisRule);
            IEnumerable<ISqlCodeAnalysisRule> rules = ruleDefinitionType.Assembly
                                                                        .GetTypes()
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