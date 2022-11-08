using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlStatementDefinitionProvider : ISqlStatementDefinitionProvider
    {
        #region Fields
        private readonly bool _isEmbedded;
        private readonly bool _limitDdlStatements;
        private readonly bool _analyzeAlways;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ISqlStatementParser _parser;
        private readonly ISqlStatementFormatter _formatter;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ISchemaDefinitionResolver _schemaDefinitionResolver;
        private readonly ILogger _logger;
        private readonly TSqlModel _model;
        private readonly IDictionary<string, SqlStatementDefinition> _definitions;
        #endregion

        #region Properties
        public IEnumerable<SqlStatementDefinition> SqlStatements => _definitions.Values;
        IEnumerable<SchemaDefinition> ISchemaProvider.Schemas => _definitions.Values;
        #endregion

        #region Constructor
        public SqlStatementDefinitionProvider
        (
            bool isEmbedded
          , bool limitDdlStatements
          , bool analyzeAlways
          , string productName
          , string areaName
          , IEnumerable<TaskItem> source
          , ISqlStatementParser parser
          , ISqlStatementFormatter formatter
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , ISchemaDefinitionResolver schemaDefinitionResolver
          , ILogger logger
          , TSqlModel model
        )
        {
            _isEmbedded = isEmbedded;
            _limitDdlStatements = limitDdlStatements;
            _analyzeAlways = analyzeAlways;
            _productName = productName;
            _areaName = areaName;
            _parser = parser;
            _formatter = formatter;
            _typeResolver = typeResolver;
            _schemaRegistry = schemaRegistry;
            _schemaDefinitionResolver = schemaDefinitionResolver;
            _logger = logger;
            _model = model;
            _definitions = new Dictionary<string, SqlStatementDefinition>();
            Collect(source.Select(x => x.GetFullPath()));
        }
        #endregion

        #region ISqlStatementDefinitionProvider Members
        public bool TryGetDefinition(string fullName, out SqlStatementDefinition definition) => _definitions.TryGetValue(fullName, out definition);
        #endregion

        #region Private Methods
        private void Collect(IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                try
                {
                    bool result = _parser.Read
                    (
                        sourceKind: SqlParserSourceKind.Stream
                      , content: File.OpenRead(file)
                      , source: file
                      , definitionName: Path.GetFileNameWithoutExtension(file)
                      , model: _model
                      , isEmbedded: _isEmbedded
                      , limitDdlStatements: _limitDdlStatements
                      , analyzeAlways: _analyzeAlways
                      , productName: _productName
                      , areaName: _areaName
                      , formatter: _formatter
                      , typeResolver: _typeResolver
                      , schemaRegistry: _schemaRegistry
                      , schemaDefinitionResolver: _schemaDefinitionResolver
                      , logger: _logger
                      , definition: out SqlStatementDefinition definition
                    );

                    if (!result)
                        continue;

                    if (_definitions.ContainsKey(definition.FullName))
                    {
                        _logger.LogError($"Ambiguous procedure definition name: {definition.FullName}", file, 0, 0);
                        continue;
                    }
                    _definitions.Add(definition.FullName, definition);
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