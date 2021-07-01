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
        private readonly string _productName;
        private readonly string _areaName;
        private readonly bool _isEmbedded;
        private readonly bool _analyzeAlways;
        private readonly ISqlStatementParser _parser;
        private readonly ISqlStatementFormatter _formatter;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;
        private readonly IEnumerable<string> _files;
        private readonly Lazy<TSqlModel> _modelAccessor;

        public PhysicalFileSqlStatementCollector
        (
            string projectName
          , bool isEmbedded
          , bool analyzeAlways
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
            this._productName = productName;
            this._areaName = areaName;
            this._parser = parser;
            this._formatter = formatter;
            this._typeResolver = typeResolver;
            this._schemaRegistry = schemaRegistry;
            this._files = files;
            this._modelAccessor = modelAccessor;
            this._logger = logger;
        }

        public override IEnumerable<SqlStatementDescriptor> CollectStatements()
        {
            return this._files.Select(this.CollectStatement).Where(x => x != null);
        }

        private SqlStatementDescriptor CollectStatement(string file)
        {
            SqlStatementDescriptor statement = new SqlStatementDescriptor
            {
                Source = file,
                Name = Path.GetFileNameWithoutExtension(file)
            };

            try
            {
                bool result = this._parser.Read
                (
                    sourceKind: SqlParserSourceKind.Stream
                  , source: File.OpenRead(file)
                  , modelAccessor: this._modelAccessor
                  , target: statement
                  , projectName: this._projectName
                  , isEmbedded: this._isEmbedded
                  , analyzeAlways: this._analyzeAlways
                  , productName: this._productName
                  , areaName: this._areaName
                  , formatter: this._formatter
                  , typeResolver: this._typeResolver
                  , schemaRegistry: this._schemaRegistry
                  , logger: this._logger
                );
                return result ? statement : null;
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