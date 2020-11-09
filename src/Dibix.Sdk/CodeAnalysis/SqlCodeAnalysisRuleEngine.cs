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
    public sealed class SqlCodeAnalysisRuleEngine : ISqlCodeAnalysisRuleEngine, ISqlCodeAnalysisSuppressionService
    {
        #region Fields
        private readonly TSqlModel _model;
        private readonly string _namingConventionPrefix;
        private readonly bool _isEmbedded;
        private readonly ILogger _logger;
        private readonly ICollection<Func<SqlCodeAnalysisContext, IEnumerable<SqlCodeAnalysisError>>> _rules;
        private readonly IDictionary<SqlCodeAnalysisSuppressionKey, SqlCodeAnalysisSuppression> _suppressionMap;
        private readonly string _resetSuppressionsFilePath;
        #endregion

        #region Constructor
        private SqlCodeAnalysisRuleEngine(TSqlModel model, string namingConventionPrefix, bool isEmbedded, ILogger logger)
        {
            this._model = model;
            this._namingConventionPrefix = namingConventionPrefix;
            this._isEmbedded = isEmbedded;
            this._logger = logger;
            this._rules = ScanRules().ToArray();
            this._resetSuppressionsFilePath = CollectResetSuppressionFilePath();
            this._suppressionMap = LoadSuppressions(this._resetSuppressionsFilePath).GroupBy(x => new SqlCodeAnalysisSuppressionKey(x.RuleName, x.Key))
                                                                                    .ToDictionary(x => x.Key, x => x.Single());
        }
        #endregion

        #region Factory Methods
        public static SqlCodeAnalysisRuleEngine Create(string databaseSchemaProviderName, string modelCollation, string namingConventionPrefix, bool isEmbedded, IEnumerable<TaskItem> source, ICollection<TaskItem> sqlReferencePath, ILogger logger)
        {
            TSqlModel model = PublicSqlDataSchemaModelLoader.Load(databaseSchemaProviderName, modelCollation, source, sqlReferencePath, logger);
            return new SqlCodeAnalysisRuleEngine(model, namingConventionPrefix, isEmbedded, logger);
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
        public bool IsSuppressed(string ruleName, string key, string hash)
        {
            bool resetSuppressions = !String.IsNullOrEmpty(this._resetSuppressionsFilePath);

            if (this._suppressionMap.TryGetValue(new SqlCodeAnalysisSuppressionKey(ruleName, key), out SqlCodeAnalysisSuppression suppression))
            {
                if (suppression.Hash == hash)
                    return true;

                if (resetSuppressions)
                {
                    suppression.Hash = hash;
                    return true;
                }
            }
            else if (resetSuppressions)
            {
                this._suppressionMap.Add(new SqlCodeAnalysisSuppressionKey(ruleName, key), new SqlCodeAnalysisSuppression(ruleName, key, hash));
                return true;
            }

            return false;
        }

        public void ResetSuppressions()
        {
            if (String.IsNullOrEmpty(this._resetSuppressionsFilePath))
                return;

            JObject json = new JObject();
            var ruleGroups = this._suppressionMap
                                 .Values
                                 .OrderBy(x => x.RuleName)
                                 .ThenBy(x => x.Key)
                                 .GroupBy(x => x.RuleName);

            foreach (IGrouping<string, SqlCodeAnalysisSuppression> ruleGroup in ruleGroups)
            {
                JObject rule = new JObject();
                foreach (SqlCodeAnalysisSuppression suppression in ruleGroup) 
                    rule.Add(suppression.Key, suppression.Hash);

                json.Add(ruleGroup.Key, rule);
            }

            using (Stream stream = File.OpenWrite(this._resetSuppressionsFilePath))
            {
                using (TextWriter textWriter = new StreamWriter(stream))
                {
                    using (JsonWriter jsonWriter = new JsonTextWriter(textWriter) { Formatting = Formatting.Indented })
                    {
                        json.WriteTo(jsonWriter);
                    }
                }
            }
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
            return new SqlCodeAnalysisContext(this._model, source, fragment, isScriptArtifact, this._namingConventionPrefix, this._isEmbedded, this, this._logger);
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

        private static IEnumerable<SqlCodeAnalysisSuppression> LoadSuppressions(string filePath)
        {
            Type type = typeof(JsonSchemaDefinition);

            Stream stream;
            if (!String.IsNullOrEmpty(filePath))
            {
                if (!File.Exists(filePath))
                    yield break;

                stream = File.OpenRead(filePath);
            }
            else
                stream = type.Assembly.GetManifestResourceStream($"{typeof(SqlCodeAnalysisRuleEngine).Namespace}.Suppressions.json");

            using (stream)
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

        private static string CollectResetSuppressionFilePath()
        {
            bool foundFlag = false;
            foreach (string arg in Environment.GetCommandLineArgs().Skip(3))
            {
                if (arg == "-s")
                {
                    foundFlag = true;
                    continue;
                }

                if (foundFlag)
                {
                    return arg;
                }
            }
            return null;
        }
        #endregion

        #region Nested Types
        private sealed class SqlCodeAnalysisSuppression
        {
            public string RuleName { get; }
            public string Key { get; }
            public string Hash { get; set; }

            public SqlCodeAnalysisSuppression(string ruleName, string key, string hash)
            {
                this.RuleName = ruleName;
                this.Key = key;
                this.Hash = hash;
            }
        }

        private readonly struct SqlCodeAnalysisSuppressionKey
        {
            public string RuleName { get; }
            public string Key { get; }

            public SqlCodeAnalysisSuppressionKey(string ruleName, string key)
            {
                this.RuleName = ruleName;
                this.Key = key;
            }
        }
        #endregion
    }
}