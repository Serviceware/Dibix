using System;
using System.IO;
using System.Security.Cryptography;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisContext
    {
        public SqlModel Model { get; }
        public TSqlFragment Fragment { get; }
        public SqlCodeAnalysisConfiguration Configuration { get; }
        public string Hash { get; }

        public SqlCodeAnalysisContext(TSqlModel model, string source, TSqlFragment fragment, bool isScriptArtifact, SqlCodeAnalysisConfiguration configuration)
        {
            this.Model = new SqlModel(model, fragment, isScriptArtifact);
            this.Hash = CalculateHash(source);
            this.Fragment = fragment;
            this.Configuration = configuration;
        }

        private static string CalculateHash(string filename)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (Stream stream = File.OpenRead(filename))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}