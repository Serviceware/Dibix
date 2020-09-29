using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisRuleEngine : ISqlCodeAnalysisRuleEngine
    {
        #region Fields
        private readonly TSqlModel _model;
        private readonly SqlCodeAnalysisConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ICollection<Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>>> _rules;
        private readonly ICollection<SqlCodeAnalysisSuppression> _suppressions;
        #endregion

        #region Constructor
        private SqlCodeAnalysisRuleEngine(TSqlModel model, SqlCodeAnalysisConfiguration configuration, ILogger logger)
        {
            this._model = model;
            this._configuration = configuration;
            this._logger = logger;
            this._rules = ScanRules().ToArray();
            this._suppressions = LoadSuppressions().ToArray();
        }
        #endregion

        #region Factory Methods
        public static SqlCodeAnalysisRuleEngine Create(string databaseSchemaProviderName, string modelCollation, string namingConventionPrefix, IEnumerable<TaskItem> source, IEnumerable<TaskItem> sqlReferencePath, ILogger logger)
        {
            TSqlModel model = PublicSqlDataSchemaModelLoader.Load(databaseSchemaProviderName, modelCollation, source, sqlReferencePath, logger);
            SqlCodeAnalysisConfiguration configuration = new SqlCodeAnalysisConfiguration(namingConventionPrefix);
            return new SqlCodeAnalysisRuleEngine(model, configuration, logger);
        }
        #endregion

        #region Public Methods
        public IEnumerable<SqlCodeAnalysisError> Analyze(string source)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(source);
            return this.Analyze(source, fragment, false);
        }

        public IEnumerable<SqlCodeAnalysisError> Analyze(string source, ISqlCodeAnalysisRule rule)
        {
            TSqlFragment fragment = ScriptDomFacade.Load(source);
            SqlCodeAnalysisContext context = this.CreateContext(source, fragment, false);
            return rule.Analyze(context);
        }

        public IEnumerable<SqlCodeAnalysisError> AnalyzeScript(string source, string content)
        {
            TSqlFragment fragment = ScriptDomFacade.Parse(content);
            return this.Analyze(source, fragment, true);
        }
        #endregion

        #region Private Methods
        private IEnumerable<SqlCodeAnalysisError> Analyze(string source, TSqlFragment fragment, bool isScriptArtifact)
        {
            SqlCodeAnalysisContext context = this.CreateContext(source, fragment, isScriptArtifact);
            return this._rules.SelectMany(x => x.Invoke(context));
        }

        private SqlCodeAnalysisContext CreateContext(string source, TSqlFragment fragment, bool isScriptArtifact)
        {
            return new SqlCodeAnalysisContext(this._model, source, fragment, isScriptArtifact, this._configuration, this._logger, this._suppressions);
        }

        private static IEnumerable<Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>>> ScanRules()
        {
            return SqlCodeAnalysisRuleMap.EnabledRules.Select(x =>
            {
                ParameterExpression contextParameter = Expression.Parameter(typeof(SqlCodeAnalysisContext), "context");
                Expression ruleInstance = Expression.New(x);
                MethodInfo analyzeMethod = typeof(ISqlCodeAnalysisRule).GetMethod(nameof(ISqlCodeAnalysisRule.Analyze));
                Expression analyzeCall = Expression.Call(ruleInstance, analyzeMethod, contextParameter);
                Expression<Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>>> lambda = Expression.Lambda<Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>>>(analyzeCall, contextParameter);
                Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>> compiled = lambda.Compile();
                return compiled;
            });
        }

        private static IEnumerable<SqlCodeAnalysisSuppression> LoadSuppressions()
        {
            Type type = typeof(JsonSchemaDefinition);
            string resourcePath = $"{typeof(SqlCodeAnalysisRuleEngine).Namespace}.Suppressions.json";
            using (Stream stream = type.Assembly.GetManifestResourceStream(resourcePath))
            {
                using (TextReader textReader = new StreamReader(stream))
                {
                    using (JsonReader jsonReader = new JsonTextReader(textReader))
                    {
                        JObject json = JObject.Load(jsonReader);
                        foreach (JProperty rule in json.Properties())
                        {
                            foreach (JProperty suppression in ((JObject)rule.Value).Properties())
                            {
                                yield return new SqlCodeAnalysisSuppression(rule.Name, suppression.Name, (string)((JValue)suppression.Value).Value);
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}