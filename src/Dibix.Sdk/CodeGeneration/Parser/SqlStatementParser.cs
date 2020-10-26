using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public bool Read(SqlParserSourceKind sourceKind, object source, Lazy<TSqlModel> modelAccessor, SqlStatementInfo target, string productName, string areaName, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            if (!SourceReaders.TryGetValue(sourceKind, out Func<object, TSqlFragment> reader))
                throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, null);

            TSqlFragment fragment = reader(source);
            TSqlElementLocator elementLocator = new TSqlElementLocator(modelAccessor, fragment, false);
            return this.CollectStatementInfo(fragment, elementLocator, target, productName, areaName, formatter, typeResolver, schemaRegistry, logger);
        }
        #endregion

        #region Private Methods
        private bool CollectStatementInfo(TSqlFragment fragment, TSqlElementLocator elementLocator, SqlStatementInfo target, string productName, string areaName, ISqlStatementFormatter formatter, ITypeResolverFacade typeResolver, ISchemaRegistry schemaRegistry, ILogger logger)
        {
            ICollection<SqlHint> hints = SqlHintParser.ReadHeader(target.Source, logger, fragment).ToArray();
            bool hasHints = hints.Any();
            bool hasNoCompileHint = hints.Any(x => x.Kind == SqlHint.NoCompile);
            bool include = (!this._requireExplicitMarkup || hasHints) && !hasNoCompileHint;
            if (!include)
                return false;

            TVisitor visitor = new TVisitor
            {
                ProductName = productName,
                AreaName = areaName,
                ElementLocator = elementLocator,
                Formatter = formatter,
                Target = target,
                TypeResolver = typeResolver,
                SchemaRegistry = schemaRegistry,
                Logger = logger
            };
            visitor.Hints.AddRange(hints);

            fragment.Accept(visitor);

            //if (visitor.Target.Content == null)
            //    logger.LogError(null, "File could not be parsed", target.Source, fragment.StartLine, fragment.StartColumn);
            return visitor.Target.Content != null;
        }

        private static TSqlFragment ReadFromString(object source) => ReadFromTextReader(new StringReader((string)source));

        private static TSqlFragment ReadFromStream(object source) => ReadFromTextReader(new StreamReader((Stream)source));

        private static TSqlFragment ReadFromAst(object source) => (TSqlFragment)source;

        private static TSqlFragment ReadFromTextReader(TextReader reader) => ScriptDomFacade.Load(reader);
        #endregion
    }
}