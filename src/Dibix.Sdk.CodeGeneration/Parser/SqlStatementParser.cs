using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SqlStatementParser<TVisitor> : ISqlStatementParser where TVisitor : SqlParserVisitor, new()
    {
        #region Fields
        private readonly bool _requireExplicitMarkup;
        #endregion

        #region Constructor
        protected SqlStatementParser(bool requireExplicitMarkup) => _requireExplicitMarkup = requireExplicitMarkup;
        #endregion

        #region ISqlStatementParser Members
        public bool Read
        (
            string filePath
          , string definitionName
          , TSqlModel model
          , bool isEmbedded
          , bool limitDdlStatements
          , bool analyzeAlways
          , string productName
          , string areaName
          , ISqlStatementFormatter formatter
          , ITypeResolverFacade typeResolver
          , ISchemaRegistry schemaRegistry
          , ILogger logger
          , out SqlStatementDefinition definition
        )
        {
            TSqlFragment fragment = ScriptDomFacade.Load(filePath);
            TSqlFragmentAnalyzer fragmentAnalyzer = TSqlFragmentAnalyzer.Create(filePath, fragment, isScriptArtifact: false, isEmbedded, limitDdlStatements, analyzeAlways, model, logger);
            return TryCollectStatementDescriptor(fragment, fragmentAnalyzer, filePath, definitionName, productName, areaName, formatter, typeResolver, schemaRegistry, logger, out definition);
        }
        #endregion

        #region Private Methods
        private bool TryCollectStatementDescriptor(TSqlFragment fragment, TSqlFragmentAnalyzer fragmentAnalyzer, string source, string definitionName, string productName, string areaName, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger, out SqlStatementDefinition definition)
        {
            ISqlMarkupDeclaration markup = SqlMarkupReader.ReadHeader(fragment, source, logger);
            bool hasMarkup = markup.HasElements;
            bool hasNoCompileElement = markup.HasSingleElement(SqlMarkupKey.NoCompile, source, logger);
            bool include = (!_requireExplicitMarkup || hasMarkup) && !hasNoCompileElement;
            if (!include)
            {
                definition = null;
                return false;
            }

            TVisitor visitor = new TVisitor
            {
                Source = source,
                DefinitionName = definitionName,
                ProductName = productName,
                AreaName = areaName,
                FragmentAnalyzer = fragmentAnalyzer,
                Formatter = formatter,
                TypeResolver = typeResolver,
                SchemaRegistry = schemaRegistry,
                Logger = logger,
                Markup = markup
            };

            fragment.Accept(visitor);

            //if (visitor.Target.Content == null)
            //    logger.LogError(null, "File could not be parsed", target.Source, fragment.StartLine, fragment.StartColumn);

            // TODO: Investigate and document use case
            //if (visitor.Definition.Statement != null)
            //{
            //    definition = visitor.Definition;
            //    return true;
            //}

            if (visitor.Definition != null)
            {
                definition = visitor.Definition;
                return true;
            }

            definition = null;
            return false;
        }
        #endregion
    }
}