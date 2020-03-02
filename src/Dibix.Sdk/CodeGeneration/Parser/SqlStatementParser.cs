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
        #endregion

        #region ISqlStatementParser Members
        public bool Read(SqlParserSourceKind sourceKind, object source, Lazy<TSqlModel> modelAccessor, SqlStatementInfo target, string productName, string areaName, ISqlStatementFormatter formatter, IContractResolverFacade contractResolver, IErrorReporter errorReporter)
        {
            if (!SourceReaders.TryGetValue(sourceKind, out Func<object, TSqlFragment> reader))
                throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, null);

            TSqlFragment fragment = reader(source);
            TSqlElementLocator elementLocator = new TSqlElementLocator(null, modelAccessor, fragment);
            return CollectStatementInfo(fragment, elementLocator, target, productName, areaName, formatter, contractResolver, errorReporter);
        }
        #endregion

        #region Private Methods
        private static TSqlFragment ReadFromString(object source) => ReadFromTextReader(new StringReader((string)source));

        private static TSqlFragment ReadFromStream(object source) => ReadFromTextReader(new StreamReader((Stream)source));

        private static TSqlFragment ReadFromAst(object source) => (TSqlFragment)source;

        private static TSqlFragment ReadFromTextReader(TextReader reader) => ScriptDomFacade.Load(reader);

        private static bool CollectStatementInfo(TSqlFragment fragment, TSqlElementLocator elementLocator, SqlStatementInfo target, string productName, string areaName, ISqlStatementFormatter formatter, IContractResolverFacade contractResolver, IErrorReporter errorReporter)
        {
            TVisitor visitor = new TVisitor
            {
                ProductName = productName,
                AreaName = areaName,
                ElementLocator = elementLocator,
                Formatter = formatter,
                Target = target,
                ContractResolver = contractResolver,
                ErrorReporter = errorReporter
            };
            visitor.Hints.AddRange(SqlHintParser.FromFragment(target.Source, errorReporter, fragment));

            fragment.Accept(visitor);

            //if (visitor.Target.Content == null)
            //    errorReporter.RegisterError(target.Source, fragment.StartLine, fragment.StartColumn, null, "File could not be parsed");
            return visitor.Target.Content != null;
        }
        #endregion
    }
}