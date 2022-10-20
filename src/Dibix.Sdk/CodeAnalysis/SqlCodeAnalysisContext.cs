using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisContext
    {
        private readonly string _source;
        private readonly ISqlCodeAnalysisSuppressionService _suppressionService;
        private readonly ILogger _logger;
        private readonly string _hash;

        public SqlModel Model { get; }
        public TSqlFragment Fragment { get; }
        public SqlCodeAnalysisConfiguration Configuration { get; }
        public bool IsEmbedded { get; }

        public SqlCodeAnalysisContext
        (
            TSqlModel model
          , string source
          , TSqlFragment fragment
          , bool isScriptArtifact
          , SqlCoreConfiguration globalConfiguration
          , SqlCodeAnalysisConfiguration codeAnalysisConfiguration
          , ISqlCodeAnalysisSuppressionService suppressionService
          , ILogger logger
        )
        {
            _source = source;
            _suppressionService = suppressionService;
            _logger = logger;
            Model = new SqlModel(source, fragment, isScriptArtifact, globalConfiguration, model, logger);
            _hash = CalculateHash(source);
            Fragment = fragment;
            Configuration = codeAnalysisConfiguration;
            IsEmbedded = globalConfiguration.IsEmbedded;
        }

        public bool IsSuppressed(string ruleName, string key) => _suppressionService.IsSuppressed(ruleName, key, _hash);

        public void LogError(string code, string text, int line, int column) => _logger.LogError(code, text, _source, line, column);

        private static string CalculateHash(string filename)
        {
            using (MD5 md5 = MD5.Create())
            {
                string input = String.Join("\n", File.ReadAllLines(filename));
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}