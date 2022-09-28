using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PhysicalFileSqlStatementCollector : SqlStatementCollector
    {
        private readonly string _projectName;
        private readonly string _rootNamespace;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly bool _isEmbedded;
        private readonly bool _limitDdlStatements;
        private readonly bool _analyzeAlways;
        private readonly ISqlStatementParser _parser;
        private readonly ISqlStatementFormatter _formatter;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ISchemaDefinitionResolver _schemaDefinitionResolver;
        private readonly ILogger _logger;
        private readonly IEnumerable<string> _files;
        private readonly Lazy<TSqlModel> _modelAccessor;

        public PhysicalFileSqlStatementCollector
        (
            string projectName
          , bool isEmbedded
          , bool limitDdlStatements
          , bool analyzeAlways
          , string rootNamespace
          , string productName
          , string areaName
          , ISqlStatementParser parser
          , ISqlStatementFormatter formatter
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , ISchemaDefinitionResolver schemaDefinitionResolver
          , ILogger logger
          , IEnumerable<string> files
          , Lazy<TSqlModel> modelAccessor
        )
        {
            this._projectName = projectName;
            this._isEmbedded = isEmbedded;
            this._limitDdlStatements = limitDdlStatements;
            this._analyzeAlways = analyzeAlways;
            this._rootNamespace = rootNamespace;
            this._productName = productName;
            this._areaName = areaName;
            this._parser = parser;
            this._formatter = formatter;
            this._typeResolver = typeResolver;
            this._schemaRegistry = schemaRegistry;
            this._files = files;
            this._modelAccessor = modelAccessor;
            this._schemaDefinitionResolver = schemaDefinitionResolver;
            this._logger = logger;
        }

        public override IEnumerable<SqlStatementDefinition> CollectStatements()
        {
            return this._files.Select(this.CollectStatement).Where(x => x != null);
        }

        private SqlStatementDefinition CollectStatement(string file)
        {
            string definitionName = Path.GetFileNameWithoutExtension(file);

            try
            {
                bool result = this._parser.Read
                (
                    sourceKind: SqlParserSourceKind.Stream
                  , content: File.OpenRead(file)
                  , source: file
                  , definitionName: definitionName
                  , modelAccessor: this._modelAccessor
                  , projectName: this._projectName
                  , isEmbedded: this._isEmbedded
                  , limitDdlStatements: this._limitDdlStatements
                  , analyzeAlways: this._analyzeAlways
                  , rootNamspace: this._rootNamespace
                  , productName: this._productName
                  , areaName: this._areaName
                  , formatter: this._formatter
                  , typeResolver: this._typeResolver
                  , schemaRegistry: this._schemaRegistry
                  , schemaDefinitionResolver: this._schemaDefinitionResolver
                  , logger: this._logger
                  , definition: out SqlStatementDefinition definition
                );
                return result ? definition : null;
            }
            catch (Exception exception)
            {
                throw new Exception($@"{exception.Message}
   at {file}
", exception);
            }
        }
    }
}