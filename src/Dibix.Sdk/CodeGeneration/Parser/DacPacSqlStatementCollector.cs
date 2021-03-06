using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DacPacSqlStatementCollector : SqlStatementCollector
    {
        private readonly string _projectName;
        private readonly string _rootNamespace;
        private readonly string _productName;
        private readonly string _areaName;
        private readonly ISqlStatementParser _parser;
        private readonly ISqlStatementFormatter _formatter;
        private readonly ITypeResolverFacade _typeResolver;
        private readonly ISchemaRegistry _schemaRegistry;
        private readonly ISchemaDefinitionResolver _schemaDefinitionResolver;
        private readonly ILogger _logger;
        private readonly string _packagePath;
        private readonly ICollection<KeyValuePair<string, string>> _procedureNames;

        public DacPacSqlStatementCollector
        (
            string projectName
          , string rootNamespace
          , string productName
          , string areaName
          , ISqlStatementParser parser
          , ISqlStatementFormatter formatter
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , ISchemaDefinitionResolver schemaDefinitionResolver
          , ILogger logger
          , string packagePath
          , ICollection<KeyValuePair<string, string>> procedureNames
        )
        {
            this._projectName = projectName;
            this._rootNamespace = rootNamespace;
            this._productName = productName;
            this._areaName = areaName;
            this._parser = parser;
            this._formatter = formatter;
            this._typeResolver = typeResolver;
            this._schemaRegistry = schemaRegistry;
            this._schemaDefinitionResolver = schemaDefinitionResolver;
            this._logger = logger;
            this._packagePath = packagePath;
            this._procedureNames = procedureNames;
        }

        public override IEnumerable<SqlStatementDefinition> CollectStatements()
        {
            TSqlModel model = TSqlModel.LoadFromDacpac(this._packagePath, new ModelLoadOptions());
            return this._procedureNames.Select(x => this.CollectStatement(x.Value, x.Key, model)).Where(x => x != null);
        }

        private SqlStatementDefinition CollectStatement(string procedureName, string displayName, TSqlModel model)
        {
            ICollection<string> parts = procedureName.Split('.').Select(x => x.Trim('[', ']')).ToArray();
            TSqlObject element = model.GetObject(ModelSchema.Procedure, new ObjectIdentifier(parts), DacQueryScopes.All);
            Guard.IsNotNull(element, nameof(element), $"The element {procedureName} could not be found in dacpac");

            string script = element.GetScript();
            bool result = this._parser.Read
            (
                sourceKind: SqlParserSourceKind.String
              , content: script
              , source: this._packagePath
              , definitionName: displayName
              , modelAccessor: new Lazy<TSqlModel>(() => model)
              , projectName: this._projectName
              , isEmbedded: false
              , analyzeAlways: false
              , rootNamspace: this._rootNamespace
              , productName: this._productName
              , areaName: this._areaName
              , formatter: this._formatter
              , typeResolver: this._typeResolver
              , schemaRegistry: this._schemaRegistry
              , schemaDefinitionResolver: this._schemaDefinitionResolver
              , logger: this._logger
              , definition: out SqlStatementDefinition definition);
            return result ? definition : null;
        }
    }
}