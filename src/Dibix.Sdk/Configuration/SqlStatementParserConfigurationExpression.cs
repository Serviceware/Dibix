﻿using System;

namespace Dibix.Sdk
{
    internal class SqlStatementParserConfigurationExpression : ISqlStatementParserConfigurationExpression
    {
        #region Fields
        private readonly ISqlStatementParser _parser;
        #endregion

        #region Constructor
        public SqlStatementParserConfigurationExpression(ISqlStatementParser parser)
        {
            this._parser = parser;
        }
        #endregion

        #region ISqlStatementParserConfigurationExpression Members
        public ISqlStatementParserConfigurationExpression Formatter<TFormatter>() where TFormatter : ISqlStatementFormatter, new()
        {
            this._parser.Formatter = new TFormatter();
            return this;
        }

        internal ISqlStatementParserConfigurationExpression Lint(Action<SqlLintConfiguration> configuration)
        {
            configuration(this._parser.LintConfiguration);
            return this;
        }
        #endregion
    }
}