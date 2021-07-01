using System;
using System.Collections.Generic;
using System.IO;
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
        public bool Read(SqlParserSourceKind sourceKind, object source, Lazy<TSqlModel> modelAccessor, SqlStatementDescriptor target, string projectName, bool isEmbedded, bool analyzeAlways, string productName, string areaName, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (!SourceReaders.TryGetValue(sourceKind, out Func<object, TSqlFragment> reader))
                throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, null);

            TSqlFragment fragment = reader(source);
            TSqlFragmentAnalyzer fragmentAnalyzer = new TSqlFragmentAnalyzer(target.Source, fragment, isScriptArtifact: false, projectName, isEmbedded, analyzeAlways, modelAccessor, logger);
            return this.CollectStatementDescriptor(fragment, fragmentAnalyzer, target, productName, areaName, formatter, typeResolver, schemaRegistry, logger);
        }
        #endregion

        #region Private Methods
        private bool CollectStatementDescriptor(TSqlFragment fragment, TSqlFragmentAnalyzer fragmentAnalyzer, SqlStatementDescriptor target, string productName, string areaName, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            ISqlMarkupDeclaration markup = SqlMarkupReader.ReadHeader(fragment, target.Source, logger);
            bool hasMarkup = markup.HasElements;
            bool hasNoCompileElement = markup.HasSingleElement(SqlMarkupKey.NoCompile, target.Source, logger);
            bool include = (!this._requireExplicitMarkup || hasMarkup) && !hasNoCompileElement;
            if (!include)
                return false;

            TVisitor visitor = new TVisitor
            {
                ProductName = productName,
                AreaName = areaName,
                FragmentAnalyzer = fragmentAnalyzer,
                Formatter = formatter,
                Target = target,
                TypeResolver = typeResolver,
                SchemaRegistry = schemaRegistry,
                Logger = logger,
                Markup = markup
            };

            fragment.Accept(visitor);

            //if (visitor.Target.Content == null)
            //    logger.LogError(null, "File could not be parsed", target.Source, fragment.StartLine, fragment.StartColumn);
            
            return visitor.Target.Statement != null;
        }

        private static TSqlFragment ReadFromString(object source) => ReadFromTextReader(new StringReader((string)source));

        private static TSqlFragment ReadFromStream(object source) => ReadFromTextReader(new StreamReader((Stream)source));

        private static TSqlFragment ReadFromAst(object source) => (TSqlFragment)source;

        private static TSqlFragment ReadFromTextReader(TextReader reader) => ScriptDomFacade.Load(reader);
        #endregion
    }
}