using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

        public SqlCodeAnalysisContext(TSqlModel model, string source, TSqlFragment fragment, bool isScriptArtifact, SqlCodeAnalysisConfiguration configuration, ISqlCodeAnalysisSuppressionService suppressionService, ILogger logger)
        {
            this._source = source;
            this._suppressionService = suppressionService;
            this._logger = logger;
            this.Model = new SqlModel(model, fragment, isScriptArtifact);
            this._hash = CalculateHash(source);
            this.Fragment = fragment;
            this.Configuration = configuration;
        }

        public bool IsSuppressed(string ruleName, string key) => this._suppressionService.IsSuppressed(ruleName, key, this._hash);

        public void LogError(string code, string text, int line, int column) => this._logger.LogError(code, text, this._source, line, column);

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