using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SqlStatementFormatter : ISqlStatementFormatter
    {
        protected const int Indentation = 4;

        private static readonly Type[] ExcludedTypes = 
        {
            typeof(PredicateSetStatement)
        };

        public bool StripWhiteSpace { get; set; }

        FormattedSqlStatement ISqlStatementFormatter.Format(SqlStatementDescriptor statementDescriptor, StatementList statementList)
        {
            FormattedSqlStatement statement = this.Format(statementDescriptor, statementList);
            statement.Content = statement.Content.Trim();
            return statement;
        }

        protected abstract FormattedSqlStatement Format(SqlStatementDescriptor statementDescriptor, StatementList statementList);

        protected void CollectStatements(StatementList statementList, Action<TSqlStatement, int, int> statementHandler, Action<TSqlParserToken> tokenHandler = null)
        {
            IList<TSqlStatement> statements = GetStatements(statementList).ToArray();
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

        private static IEnumerable<TSqlStatement> GetStatements(StatementList statementList) => statementList.Statements.Where(FilterStatement);

        private static bool FilterStatement(TSqlStatement statement) => !ExcludedTypes.Any(statement.GetType().IsAssignableFrom);
    }
}