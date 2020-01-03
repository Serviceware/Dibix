using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class DatabaseAccessorStatementProvider : IDatabaseAccessorStatementProvider
    {
        private readonly ISqlStatementParser _parser;
        private readonly ISqlStatementFormatter _formatter;
        private readonly IContractResolverFacade _contractResolverFacade;
        private readonly IErrorReporter _errorReporter;
        private readonly string _projectDirectory;
        private readonly bool _generatePublicArtifacts;
        private readonly bool _embedStatements;
        private readonly string _productName;
        private readonly string _areaName;

        public DatabaseAccessorStatementProvider(ISqlStatementParser parser, ISqlStatementFormatter formatter, IContractResolverFacade contractResolver, IErrorReporter errorReporter, string projectDirectory, bool generatePublicArtifacts, bool embedStatements, string productName, string areaName)
        {
            this._parser = parser;
            this._formatter = formatter;
            this._contractResolverFacade = contractResolver;
            this._errorReporter = errorReporter;
            this._projectDirectory = projectDirectory;
            this._generatePublicArtifacts = generatePublicArtifacts;
            this._embedStatements = embedStatements;
            this._productName = productName;
            this._areaName = areaName;
        }

        public IEnumerable<SqlStatementInfo> CollectStatements(IEnumerable<string> sources)
        {
            return sources.Where(x => MatchFile(this._projectDirectory, x, this._embedStatements, this._errorReporter))
                          .Select(source => SqlStatementParser.ParseStatement(source, this._productName, this._areaName, this._parser, this._formatter, this._contractResolverFacade, this._errorReporter))
                          .Where(x => x != null);
        }

        private static bool MatchFile(string projectDirectory, string relativeFilePath, bool embedStatements, IErrorReporter errorReporter)
        {
            string inputFilePath = Path.Combine(projectDirectory, relativeFilePath);
            ICollection<SqlHint> hints = SqlHintParser.FromFile(inputFilePath, errorReporter).ToArray();
            bool hasHints = hints.Any();
            bool hasNoCompileHint = hints.Any(x => x.Kind == SqlHint.NoCompile);
            return (embedStatements || hasHints) && !hasNoCompileHint;
        }
    }
}