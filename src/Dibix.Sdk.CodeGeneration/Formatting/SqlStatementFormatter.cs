using System;
using System.Collections.Generic;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SqlStatementFormatter : ISqlStatementFormatter
    {
        protected const int Indentation = 4;
        
        public bool StripWhiteSpace { get; set; }

        FormattedSqlStatement ISqlStatementFormatter.Format(SqlStatementDefinition statementDefinition, StatementList statementList)
        {
            FormattedSqlStatement statement = this.Format(statementDefinition, statementList);
            statement.Content = statement.Content.Trim();
            return statement;
        }

        protected abstract FormattedSqlStatement Format(SqlStatementDefinition statementDefinition, StatementList statementList);

        protected void CollectStatements(StatementList statementList, Action<TSqlStatement, int, int> statementHandler, Action<TSqlParserToken> tokenHandler = null)
        {
            IList<TSqlStatement> statements = statementList.Statements;
            for (int i = 0; i < statements.Count; i++)
            {
                TSqlStatement statement = statements[i];
                int startIndex = statement.GetFirstTokenIndex();
                int endIndex = statement.LastTokenIndex;

                bool gotWhiteSpace = false;
                for (int j = startIndex; j <= endIndex; j++)
                {
                    TSqlParserToken token = statement.ScriptTokenStream[j];

                    if (token.TokenType == TSqlTokenType.WhiteSpace)
                    {
                        if (this.StripWhiteSpace)
                            token.Text = !gotWhiteSpace ? " " : null; // Replace subsequent whitespace with one space
                        else
                            token.Text = token.Text.Replace("\t", new string(' ', Indentation));

                        gotWhiteSpace = true;
                    }
                    else
                        gotWhiteSpace = false;

                    tokenHandler?.Invoke(token);
                }

                statementHandler(statement, i, statements.Count);
            }
        }
    }
}