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

        public SqlModel Model { get; }
        public TSqlFragment Fragment { get; }
        public SqlCodeAnalysisConfiguration Configuration { get; }

        public SqlCodeAnalysisContext
        (
            TSqlModel model
          , string source
          , TSqlFragment fragment
          , bool isScriptArtifact
          , SqlCodeAnalysisConfiguration configuration
          , ISqlCodeAnalysisSuppressionService suppressionService
          , ILogger logger
        )
        {
            _source = source;
            _suppressionService = suppressionService;
            _logger = logger;
            Model = new SqlModel(source, fragment, isScriptArtifact, configuration.IsEmbedded, configuration.LimitDdlStatements, model, logger);
            Fragment = fragment;
            Configuration = configuration;
        }

        public bool IsSuppressed(string ruleName, string key) => _suppressionService.IsSuppressed(ruleName, key, _source);

        public void LogError(string code, string text, int line, int column) => _logger.LogError(code, text, _source, line, column);
    }
}