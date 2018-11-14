using System.Collections.Generic;
using System.IO;
using Dibix.Sdk.CodeGeneration.Parser;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SqlStatementParser<TVisitor> : ISqlStatementParser where TVisitor : SqlParserVisitor, new()
    {
        #region Fields
        private readonly SqlCodeAnalysisGeneratorAdapter _codeAnalysisRunner;
        #endregion

        #region Properties
        public ISqlStatementFormatter Formatter { get; set; }
        #endregion

        #region Constructor
        protected SqlStatementParser()
        {
            this._codeAnalysisRunner = new SqlCodeAnalysisGeneratorAdapter();
        }
        #endregion

        #region ISqlStatementParser Members
        public void Read(IExecutionEnvironment environment, Stream source, SqlStatementInfo target)
        {
            string sourceFilePath = source is FileStream file ? file.Name : null;
            using (TextReader reader = new StreamReader(source))
            {
                TSqlParser parser = new TSql140Parser(true);
                IList<ParseError> parseErrors;
                TSqlFragment fragment = parser.Parse(reader, out parseErrors);
                if (this._codeAnalysisRunner.Analyze(environment, fragment, sourceFilePath))
                    return;

                CollectStatementInfo(fragment, target, this.Formatter, environment);
            }
        }
        #endregion

        #region Private Methods
        private static void CollectStatementInfo(TSqlFragment fragment, SqlStatementInfo target, ISqlStatementFormatter formatter, IExecutionEnvironment environment)
        {
            TVisitor visitor = new TVisitor
            {
                Formatter = formatter,
                Target = target,
                Environment = environment
            };

            fragment.Accept(visitor);

            if (visitor.Target.Content == null)
                environment.RegisterError(target.SourcePath, fragment.StartLine, fragment.StartColumn, null, "File could not be parsed");
        }
        #endregion
    }
}