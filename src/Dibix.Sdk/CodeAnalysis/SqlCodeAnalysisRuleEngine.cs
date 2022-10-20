using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisRuleEngine : ISqlCodeAnalysisRuleEngine, ISqlCodeAnalysisSuppressionService
    {
        #region Fields
        private const string LockSectionName = "SqlCodeAnalysis";
        private readonly TSqlModel _model;
        private readonly SqlCoreConfiguration _globalConfiguration;
        private readonly SqlCodeAnalysisConfiguration _codeAnalysisConfiguration;
        private readonly LockEntryManager _lockEntryManager;
        private readonly ILogger _logger;
        #endregion

        #region Constructor
        private SqlCodeAnalysisRuleEngine(TSqlModel model, SqlCoreConfiguration globalConfiguration, SqlCodeAnalysisConfiguration codeAnalysisConfiguration, LockEntryManager lockEntryManager, ILogger logger)
        {
            _model = model;
            _globalConfiguration = globalConfiguration;
            _codeAnalysisConfiguration = codeAnalysisConfiguration;
            _lockEntryManager = lockEntryManager;
            _logger = logger;
        }
        #endregion

        #region Factory Methods
        public static SqlCodeAnalysisRuleEngine Create(TSqlModel model, SqlCoreConfiguration globalConfiguration, SqlCodeAnalysisConfiguration codeAnalysisConfiguration, LockEntryManager lockEntryManager, ILogger logger)
        {
            return new SqlCodeAnalysisRuleEngine(model, globalConfiguration, codeAnalysisConfiguration, lockEntryManager, logger);
        }
        #endregion

        #region ISqlCodeAnalysisRuleEngine Members
        public IEnumerable<SqlCodeAnalysisError> Analyze(string source) => Analyze(source, SqlCodeAnalysisRuleMap.EnabledRules);
        public IEnumerable<SqlCodeAnalysisError> Analyze(string source, Type ruleType) => Analyze(source, EnumerableExtensions.Create(ruleType));

        public IEnumerable<SqlCodeAnalysisError> AnalyzeScript(string source, string content)
        {
            TSqlFragment fragment = ScriptDomFacade.Parse(content);
            return Analyze(source, fragment, isScriptArtifact: true, SqlCodeAnalysisRuleMap.EnabledRules);
        }
        #endregion

        #region ISqlCodeAnalysisSuppressionService Members
        public bool IsSuppressed(string ruleName, string key, string hash) => _lockEntryManager.HasEntry(sectionName: LockSectionName, groupName: ruleName, recordName: key, signature: hash);
        #endregion

        #region Private Methods
        private IEnumerable<SqlCodeAnalysisError> Analyze(string source, IEnumerable<Type> rules)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(source);
            return Analyze(source, fragment, isScriptArtifact: false, rules);
        }
        private IEnumerable<SqlCodeAnalysisError> Analyze(string source, TSqlFragment fragment, bool isScriptArtifact, IEnumerable<Type> rules)
        {
            SqlCodeAnalysisContext context = CreateContext(source, fragment, isScriptArtifact);
            IEnumerable<SqlCodeAnalysisError> AnalyzeRule(Type ruleType)
            {
                Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>> handler = SqlCodeAnalysisRuleMap.GetHandler(ruleType);
                IEnumerable<SqlCodeAnalysisError> errors = handler(context);
                return errors;
            }
            return rules.SelectMany(AnalyzeRule);
        }

        private SqlCodeAnalysisContext CreateContext(string source, TSqlFragment fragment, bool isScriptArtifact)
        {
            return new SqlCodeAnalysisContext(_model, source, fragment, isScriptArtifact, _globalConfiguration, _codeAnalysisConfiguration, this, _logger);
        }
        #endregion
    }
}