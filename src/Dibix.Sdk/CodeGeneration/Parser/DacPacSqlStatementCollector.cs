using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DacPacSqlStatementCollector : SqlStatementCollector
    {
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ISqlStatementParser _parser;
        private readonly ISqlStatementFormatter _formatter;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ILogger _logger;
        private readonly string _packagePath;
        private readonly ICollection<KeyValuePair<string, string>> _procedureNames;

        public DacPacSqlStatementCollector
        (
            string productName
          , string areaName
          , ISqlStatementParser parser
          , ISqlStatementFormatter formatter
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , ILogger logger
          , string packagePath
          , ICollection<KeyValuePair<string, string>> procedureNames)
        {
            this._productName = productName;
            this._areaName = areaName;
            this._parser = parser;
            this._formatter = formatter;
            this._typeResolver = typeResolver;
            this._schemaRegistry = schemaRegistry;
            this._logger = logger;
            this._packagePath = packagePath;
            this._procedureNames = procedureNames;
        }

        public override IEnumerable<SqlStatementInfo> CollectStatements()
        {
            TSqlModel model = TSqlModel.LoadFromDacpac(this._packagePath, new ModelLoadOptions());
            return this._procedureNames.Select(x => this.CollectStatement(x.Value, x.Key, model)).Where(x => x != null);
        }

        private SqlStatementInfo CollectStatement(string procedureName, string displayName, TSqlModel model)
        {
            ICollection<string> parts = procedureName.Split('.').Select(x => x.Trim('[', ']')).ToArray();
            TSqlObject element = model.GetObject(ModelSchema.Procedure, new ObjectIdentifier(parts), DacQueryScopes.All);
            Guard.IsNotNull(element, nameof(element), $"The element {procedureName} could not be found in dacpac");

            string script = element.GetScript();
            SqlStatementInfo statement = new SqlStatementInfo
            {
                Name = displayName,
                Source = this._packagePath
            };

            bool result = this._parser.Read(SqlParserSourceKind.String, script, new Lazy<TSqlModel>(() => model), statement, this._productName, this._areaName, this._formatter, this._typeResolver, this._schemaRegistry, this._logger);
            return result ? statement : null;
        }
    }
}