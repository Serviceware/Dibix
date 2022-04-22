using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlStatementDefinitionProvider : ISqlStatementDefinitionProvider
    {
        #region Fields
        private readonly string _projectName;
        private readonly bool _isEmbedded;
        private readonly bool _analyzeAlways;
        private readonly string _rootNamespace;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ISqlStatementParser _parser;
        private readonly ISqlStatementFormatter _formatter;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;
        private readonly Lazy<TSqlModel> _modelAccessor;
        private readonly IDictionary<string, SqlStatementDefinition> _definitions;
        #endregion

        #region Properties
        public IEnumerable<SqlStatementDefinition> SqlStatements => this._definitions.Values;
        IEnumerable<SchemaDefinition> ISchemaProvider.Schemas => this._definitions.Values;
        #endregion

        #region Constructor
        public SqlStatementDefinitionProvider
        (
            string projectName
          , bool isEmbedded
          , bool analyzeAlways
          , string rootNamespace
          , string productName
          , string areaName
          , ISqlStatementParser parser
          , ISqlStatementFormatter formatter
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , ILogger logger
          , IEnumerable<string> files
          , Lazy<TSqlModel> modelAccessor
        )
        {
            this._projectName = projectName;
            this._isEmbedded = isEmbedded;
            this._analyzeAlways = analyzeAlways;
            this._rootNamespace = rootNamespace;
            this._productName = productName;
            this._areaName = areaName;
            this._parser = parser;
            this._formatter = formatter;
            this._typeResolver = typeResolver;
            this._schemaRegistry = schemaRegistry;
            this._logger = logger;
            this._modelAccessor = modelAccessor;
            this._definitions = new Dictionary<string, SqlStatementDefinition>();
            this.Collect(files);
        }
        #endregion

        #region ISqlStatementDefinitionProvider Members
        public bool TryGetDefinition(string fullName, out SqlStatementDefinition definition) => this._definitions.TryGetValue(fullName, out definition);
        #endregion

        #region Private Methods
        private void Collect(IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                try
                {
                    bool result = this._parser.Read
                    (
                        sourceKind: SqlParserSourceKind.Stream
                      , content: File.OpenRead(file)
                      , source: file
                      , definitionName: Path.GetFileNameWithoutExtension(file)
                      , modelAccessor: this._modelAccessor
                      , projectName: this._projectName
                      , isEmbedded: this._isEmbedded
                      , analyzeAlways: this._analyzeAlways
                      , rootNamspace: this._rootNamespace
                      , productName: this._productName
                      , areaName: this._areaName
                      , formatter: this._formatter
                      , typeResolver: this._typeResolver
                      , schemaRegistry: this._schemaRegistry
                      , logger: this._logger
                      , definition: out SqlStatementDefinition definition
                    );

                    if (!result)
                        continue;

                    if (this._definitions.ContainsKey(definition.FullName))
                    {
                        this._logger.LogError(null, $"Ambiguous procedure definition name: {definition.FullName}", file, 0, 0);
                        continue;
                    }
                    this._definitions.Add(definition.FullName, definition);
                }
                catch (Exception exception)
                {
                    throw new Exception($@"{exception.Message}
   at {file}
", exception);
                }
            }
        }
        #endregion
    }
}