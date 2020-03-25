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
        private readonly TSqlModel _model;
        private readonly SqlCodeAnalysisConfiguration _configuration;
        private readonly ICollection<ISqlCodeAnalysisRule> _rules;
        #endregion

        #region Constructor
        private SqlCodeAnalysisRuleEngine(TSqlModel model, SqlCodeAnalysisConfiguration configuration)
        {
            this._model = model;
            this._configuration = configuration;
            this._rules = ScanRules();
        }
        #endregion

        #region Factory Methods
        public static SqlCodeAnalysisRuleEngine Create(string namingConventionPrefix, string databaseSchemaProviderName, string modelCollation, IEnumerable<TaskItem> source, IEnumerable<TaskItem> sqlReferencePath, ILogger logger)
        {
            TSqlModel model = PublicSqlDataSchemaModelLoader.Load(databaseSchemaProviderName, modelCollation, source, sqlReferencePath, logger);
            SqlCodeAnalysisConfiguration configuration = new SqlCodeAnalysisConfiguration(namingConventionPrefix);
            return new SqlCodeAnalysisRuleEngine(model, configuration);
        }
        #endregion

        #region Public Methods
        public IEnumerable<SqlCodeAnalysisError> Analyze(TSqlFragment fragment, bool isScriptArtifact)
        {
            return this._rules.SelectMany(x => x.Analyze(this._model, fragment, this._configuration, isScriptArtifact));
        }

        public IEnumerable<SqlCodeAnalysisError> Analyze(string scriptFilePath)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(scriptFilePath);
            return this.Analyze(fragment, false);
        }

        public IEnumerable<SqlCodeAnalysisError> Analyze(string scriptFilePath, ISqlCodeAnalysisRule rule)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(scriptFilePath);
            return rule.Analyze(this._model, fragment, this._configuration, false);
        }

        public IEnumerable<SqlCodeAnalysisError> AnalyzeScript(string scriptContent)
        {
            TSqlFragment fragment = ScriptDomFacade.Parse(scriptContent);
            return this.Analyze(fragment, true);
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