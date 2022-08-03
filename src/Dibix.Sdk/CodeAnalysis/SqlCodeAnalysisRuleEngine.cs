using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
        private readonly string _projectName;
        private readonly SqlCodeAnalysisConfiguration _configuration;
        private readonly bool _isEmbedded;
        private readonly LockEntryManager _lockEntryManager;
        private readonly ILogger _logger;
        private readonly ICollection<Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>>> _rules;
        #endregion

        #region Constructor
        private SqlCodeAnalysisRuleEngine(TSqlModel model, string projectName, SqlCodeAnalysisConfiguration configuration, bool isEmbedded, LockEntryManager lockEntryManager, ILogger logger)
        {
            this._model = model;
            this._projectName = projectName;
            this._configuration = configuration;
            this._isEmbedded = isEmbedded;
            this._lockEntryManager = lockEntryManager;
            this._logger = logger;
            this._rules = ScanRules().ToArray();
        }
        #endregion

        #region Factory Methods
        public static SqlCodeAnalysisRuleEngine Create(TSqlModel model, string projectName, SqlCodeAnalysisConfiguration configuration, bool isEmbedded, LockEntryManager lockEntryManager, ILogger logger)
        {
            return new SqlCodeAnalysisRuleEngine(model, projectName, configuration, isEmbedded, lockEntryManager, logger);
        }
        #endregion

        #region ISqlCodeAnalysisRuleEngine Members
        public IEnumerable<SqlCodeAnalysisError> Analyze(string source)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(source);
            return this.Analyze(source, fragment, false);
        }

        public IEnumerable<SqlCodeAnalysisError> Analyze(string source, ISqlCodeAnalysisRule rule)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(source);
            SqlCodeAnalysisContext context = this.CreateContext(source, fragment, isScriptArtifact: false);
            return rule.Analyze(context);
        }

        public IEnumerable<SqlCodeAnalysisError> AnalyzeScript(string source, string content)
        {
            TSqlFragment fragment = ScriptDomFacade.Parse(content);
            return this.Analyze(source, fragment, true);
        }
        #endregion

        #region ISqlCodeAnalysisSuppressionService Members
        public bool IsSuppressed(string ruleName, string key, string hash) => this._lockEntryManager.HasEntry(sectionName: LockSectionName, groupName: ruleName, recordName: key, signature: hash);
        #endregion

        #region Private Methods
        private IEnumerable<SqlCodeAnalysisError> Analyze(string source, TSqlFragment fragment, bool isScriptArtifact)
        {
            SqlCodeAnalysisContext context = this.CreateContext(source, fragment, isScriptArtifact);
            return this._rules.SelectMany(x => x.Invoke(context));
        }

        private SqlCodeAnalysisContext CreateContext(string source, TSqlFragment fragment, bool isScriptArtifact)
        {
            return new SqlCodeAnalysisContext(this._model, source, fragment, isScriptArtifact, this._projectName, this._configuration, this._isEmbedded, this, this._logger);
        }

        private static IEnumerable<Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>>> ScanRules()
        {
            return SqlCodeAnalysisRuleMap.EnabledRules.Select(x =>
            {
                ParameterExpression contextParameter = Expression.Parameter(typeof(SqlCodeAnalysisContext), "context");
                Expression ruleInstance = Expression.New(x);
                MethodInfo analyzeMethod = typeof(ISqlCodeAnalysisRule).SafeGetMethod(nameof(ISqlCodeAnalysisRule.Analyze));
                Expression analyzeCall = Expression.Call(ruleInstance, analyzeMethod, contextParameter);
                Expression<Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>>> lambda = Expression.Lambda<Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>>>(analyzeCall, contextParameter);
                Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>> compiled = lambda.Compile();
                return compiled;
            });
        }
        #endregion
    }
}