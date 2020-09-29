using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly IDictionary<string, ICollection<SqlCodeAnalysisSuppression>> _suppressionMap;

        public SqlModel Model { get; }
        public TSqlFragment Fragment { get; }
        public SqlCodeAnalysisConfiguration Configuration { get; }
        public string Hash { get; }

        public SqlCodeAnalysisContext(TSqlModel model, string source, TSqlFragment fragment, bool isScriptArtifact, SqlCodeAnalysisConfiguration configuration, ILogger logger, IEnumerable<SqlCodeAnalysisSuppression> suppressions)
        {
            this._source = source;
            this._logger = logger;
            this._suppressionMap = suppressions.GroupBy(x => x.RuleName).ToDictionary(x => x.Key, x => (ICollection<SqlCodeAnalysisSuppression>)x.ToArray());
            this.Model = new SqlModel(model, fragment, isScriptArtifact);
            this.Hash = CalculateHash(source);
            this.Fragment = fragment;
            this.Configuration = configuration;
        }

        public void LogError(string code, string text, int line, int column) => this._logger.LogError(code, text, this._source, line, column);
        public IEnumerable<SqlCodeAnalysisSuppression> GetSuppressions(string ruleName)
        {
            if (!this._suppressionMap.TryGetValue(ruleName, out ICollection<SqlCodeAnalysisSuppression> suppressions))
                return Enumerable.Empty<SqlCodeAnalysisSuppression>();

            return suppressions;
        }

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