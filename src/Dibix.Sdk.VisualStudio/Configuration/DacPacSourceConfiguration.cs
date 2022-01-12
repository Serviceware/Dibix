using System.Collections.Generic;
using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.VisualStudio
{
    internal sealed class DacPacSourceConfiguration : InputSourceConfiguration
    {
        #region Fields
        private readonly string _packagePath;
        private readonly CodeGenerationModel _model;
        private readonly ICollection<KeyValuePair<string, string>> _procedureNames;
        #endregion

        #region Constructor
        public DacPacSourceConfiguration(IFileSystemProvider fileSystemProvider, string packagePath, CodeGenerationModel model)
        {
            this._packagePath = new PhysicalFileSystemProvider(fileSystemProvider.CurrentDirectory).GetPhysicalFilePath(null, packagePath);
            this._model = model;
            this._procedureNames = new HashSet<KeyValuePair<string, string>>();
        }
        #endregion

        #region Public Methods
        public void AddStoredProcedure(string procedureName, string displayName) => this._procedureNames.Add(new KeyValuePair<string, string>(displayName, procedureName));
        #endregion

        #region Overrides
        protected override IEnumerable<SqlStatementDescriptor> CollectStatements(ISqlStatementParser parser, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            SqlStatementCollector statementCollector = new DacPacSqlStatementCollector
            (
                projectName: null
              , rootNamespace: this._model.RootNamespace
              , productName: this._model.ProductName
              , areaName: this._model.AreaName
              , parser: parser
              , formatter: formatter
              , typeResolver: typeResolver
              , schemaRegistry: schemaRegistry
              , logger: logger
              , packagePath: this._packagePath
              , procedureNames: this._procedureNames
            );
            return statementCollector.CollectStatements();
        }
        #endregion
    }
}