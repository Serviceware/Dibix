using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PhysicalFileSqlStatementCollector : SqlStatementCollector
    {
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ISqlStatementParser _parser;
        private readonly ISqlStatementFormatter _formatter;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;
        private readonly IEnumerable<string> _files;
        private readonly Lazy<TSqlModel> _modelAccessor;

        public PhysicalFileSqlStatementCollector
        (
            string productName
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

        public override IEnumerable<SqlStatementInfo> CollectStatements()
        {
            return this._files.Select(this.CollectStatement).Where(x => x != null);
        }

        private SqlStatementInfo CollectStatement(string file)
        {
            SqlStatementInfo statement = new SqlStatementInfo
            {
                Source = file,
                Name = Path.GetFileNameWithoutExtension(file)
            };

            bool result = this._parser.Read(SqlParserSourceKind.Stream, File.OpenRead(file), this._modelAccessor, statement, this._productName, this._areaName, this._formatter, this._typeResolver, this._schemaRegistry, this._logger);

            return result ? statement : null;
        }
    }
}