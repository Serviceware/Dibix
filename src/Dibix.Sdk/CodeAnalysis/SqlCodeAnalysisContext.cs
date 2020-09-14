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
        private readonly ILogger _logger;

        public SqlModel Model { get; }
        public TSqlFragment Fragment { get; }
        public SqlCodeAnalysisConfiguration Configuration { get; }
        public string Hash { get; }

        public SqlCodeAnalysisContext(TSqlModel model, string source, TSqlFragment fragment, bool isScriptArtifact, SqlCodeAnalysisConfiguration configuration, ILogger logger)
        {
            this._source = source;
            this._logger = logger;
            this.Model = new SqlModel(model, fragment, isScriptArtifact);
            this.Hash = CalculateHash(source);
            this.Fragment = fragment;
            this.Configuration = configuration;
        }

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