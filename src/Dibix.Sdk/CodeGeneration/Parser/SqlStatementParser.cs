using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SqlStatementParser<TVisitor> : ISqlStatementParser, ISqlAnalysisRunner where TVisitor : SqlParserVisitor, new()
    {
        #region Fields
        private static readonly IDictionary<SqlParserSourceKind, Func<object, TSqlFragment>> SourceReaders = new Dictionary<SqlParserSourceKind, Func<object, TSqlFragment>>
        {
            { SqlParserSourceKind.String, ReadFromString },
            { SqlParserSourceKind.Stream, ReadFromStream },
            { SqlParserSourceKind.Ast, ReadFromAst },
        };
        private readonly SqlCodeAnalysisGeneratorAdapter _codeAnalysisRunner;
        #endregion

        #region Properties
        public bool IsEnabled { get; set; } = true;
        #endregion

        #region Constructor
        protected SqlStatementParser()
        {
            this._codeAnalysisRunner = new SqlCodeAnalysisGeneratorAdapter();
        }
        #endregion

        #region ISqlStatementParser Members
        public bool Read(SqlParserSourceKind sourceKind, object source, SqlStatementInfo target, ISqlStatementFormatter formatter, IContractResolverFacade contractResolverFacade, IErrorReporter errorReporter)
        {
            if (!SourceReaders.TryGetValue(sourceKind, out Func<object, TSqlFragment> reader))
                throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, null);

            TSqlFragment fragment = reader(source);
            if (this.IsEnabled && this._codeAnalysisRunner.Analyze(fragment, target.Source, errorReporter))
                return false;

            return CollectStatementInfo(fragment, target, formatter, contractResolverFacade, errorReporter);
        }
        #endregion

        #region Private Methods
        private static TSqlFragment ReadFromString(object source) => ReadFromTextReader(new StringReader((string)source));

        private static TSqlFragment ReadFromStream(object source) => ReadFromTextReader(new StreamReader((Stream)source));

        private static TSqlFragment ReadFromAst(object source) => (TSqlFragment)source;

        private static TSqlFragment ReadFromTextReader(TextReader reader)
        {
            using (reader)
            {
                TSqlParser parser = new TSql140Parser(true);
                return parser.Parse(reader, out IList<ParseError> _);
            }
        }

        private static bool CollectStatementInfo(TSqlFragment fragment, SqlStatementInfo target, ISqlStatementFormatter formatter, IContractResolverFacade contractResolverFacade, IErrorReporter errorReporter)
        {
            TVisitor visitor = new TVisitor
            {
                Formatter = formatter,
                Target = target,
                ContractResolverFacade = contractResolverFacade,
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