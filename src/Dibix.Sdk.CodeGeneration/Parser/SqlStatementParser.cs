using System;
using System.Collections.Generic;
using System.IO;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SqlStatementParser<TVisitor> : ISqlStatementParser where TVisitor : SqlParserVisitor, new()
    {
        #region Fields
        private static readonly IDictionary<SqlParserSourceKind, Func<object, TSqlFragment>> SourceReaders = new Dictionary<SqlParserSourceKind, Func<object, TSqlFragment>>
        {
            { SqlParserSourceKind.String, ReadFromString },
            { SqlParserSourceKind.Stream, ReadFromStream },
            { SqlParserSourceKind.Ast, ReadFromAst },
        };
        private readonly bool _requireExplicitMarkup;
        #endregion

        #region Constructor
        protected SqlStatementParser(bool requireExplicitMarkup) => this._requireExplicitMarkup = requireExplicitMarkup;
        #endregion

        #region ISqlStatementParser Members
        public bool Read
        (
              SqlParserSourceKind sourceKind
            , object content
            , string source
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
            , ISchemaDefinitionResolver schemaDefinitionResolver
            , ILogger logger
            , out SqlStatementDefinition definition
        )
        {
            if (!SourceReaders.TryGetValue(sourceKind, out Func<object, TSqlFragment> reader))
                throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, null);

            TSqlFragment fragment = reader(content);
            TSqlFragmentAnalyzer fragmentAnalyzer = TSqlFragmentAnalyzer.Create(source, fragment, isScriptArtifact: false, isEmbedded, limitDdlStatements, analyzeAlways, model, logger);
            return this.TryCollectStatementDescriptor(fragment, fragmentAnalyzer, source, definitionName, productName, areaName, formatter, typeResolver, schemaRegistry, schemaDefinitionResolver, logger, out definition);
        }
        #endregion

        #region Private Methods
        private bool TryCollectStatementDescriptor(TSqlFragment fragment, TSqlFragmentAnalyzer fragmentAnalyzer, string source, string definitionName, string productName, string areaName, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ISchemaDefinitionResolver schemaDefinitionResolver, ILogger logger, out SqlStatementDefinition definition)
        {
            ISqlMarkupDeclaration markup = SqlMarkupReader.ReadHeader(fragment, source, logger);
            bool hasMarkup = markup.HasElements;
            bool hasNoCompileElement = markup.HasSingleElement(SqlMarkupKey.NoCompile, source, logger);
            bool include = (!this._requireExplicitMarkup || hasMarkup) && !hasNoCompileElement;
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
                SchemaDefinitionResolver = schemaDefinitionResolver,
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

        private static TSqlFragment ReadFromString(object source) => ReadFromTextReader(new StringReader((string)source));

        private static TSqlFragment ReadFromStream(object source) => ReadFromTextReader(new StreamReader((Stream)source));

        private static TSqlFragment ReadFromAst(object source) => (TSqlFragment)source;

        private static TSqlFragment ReadFromTextReader(TextReader reader) => ScriptDomFacade.Load(reader);
        #endregion
    }
}