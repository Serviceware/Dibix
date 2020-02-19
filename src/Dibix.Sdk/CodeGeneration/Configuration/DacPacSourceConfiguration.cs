using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DacPacSourceConfiguration : InputSourceConfiguration
    {
        #region Fields
        private readonly string _packagePath;
        private readonly ICollection<KeyValuePair<string, string>> _procedureNames;
        #endregion

        #region Constructor
        public DacPacSourceConfiguration(IFileSystemProvider fileSystemProvider, string packagePath)
        {
            this._packagePath = new PhysicalFileSystemProvider(fileSystemProvider.CurrentDirectory).GetPhysicalFilePath(null, packagePath);
            this._procedureNames = new HashSet<KeyValuePair<string, string>>();
        }
        #endregion

        #region Public Methods
        public void AddStoredProcedure(string procedureName, string displayName) => this._procedureNames.Add(new KeyValuePair<string, string>(displayName, procedureName));
        #endregion

        #region Overrides
        protected override IEnumerable<SqlStatementInfo> CollectStatements(ISqlStatementParser parser, ISqlStatementFormatter formatter, IContractResolverFacade contractResolver, IErrorReporter errorReporter)
        {
            SqlStatementCollector statementCollector = new DacPacSqlStatementCollector
            (
                productName: null
              , areaName: null
              , parser: parser
              , formatter: formatter
              , contractResolver: contractResolver
              , errorReporter: errorReporter
              , packagePath: this._packagePath
              , procedureNames: this._procedureNames
            );
            return statementCollector.CollectStatements();
        }
        #endregion
    }
}