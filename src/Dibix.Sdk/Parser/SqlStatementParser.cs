﻿using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    public abstract class SqlStatementParser<TVisitor> : ISqlStatementParser where TVisitor : SqlParserVisitor, new()
    {
        #region Properties
        public SqlLintConfiguration LintConfiguration { get; }
        public ISqlStatementFormatter Formatter { get; set; }
        #endregion

        #region Constructor
        protected SqlStatementParser()
        {
            this.LintConfiguration = new SqlLintConfiguration();
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
                RunLint(fragment, this.LintConfiguration, sourceFilePath);
                CollectStatementInfo(fragment, target, this.Formatter, environment);
            }
        }
        #endregion

        #region Private Methods
        private static void RunLint(TSqlFragment fragment, SqlLintConfiguration configuration, string sourceFilePath)
        {
            if (!configuration.IsEnabled)
                return;

            configuration.Rules.Each(x => x.Execute(fragment, sourceFilePath));
        }

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