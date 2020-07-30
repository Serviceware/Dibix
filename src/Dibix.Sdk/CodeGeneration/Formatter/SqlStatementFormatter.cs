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

        public abstract string Format(SqlStatementInfo info, StatementList statementList);

        protected void CollectStatements(StatementList statementList, Action<TSqlStatement, int, int> statementHandler, Action<TSqlParserToken> tokenHandler = null)
        {
            IList<TSqlStatement> statements = GetStatements(statementList).ToArray();
            for (int i = 0; i < statements.Count; i++)
            {
                TSqlStatement statement = statements[i];
                int startIndex = statement.GetFirstTokenIndex();
                int endIndex = statement.LastTokenIndex;
                for (int j = startIndex; j <= endIndex; j++)
                {
                    TSqlParserToken token = statement.ScriptTokenStream[j];

                    if (token.TokenType == TSqlTokenType.WhiteSpace)
                        token.Text = token.Text.Replace("\t", new string(' ', Indentation));

                    tokenHandler?.Invoke(token);
                }

                statementHandler(statement, i, statements.Count);
            }
        }

        private static IEnumerable<TSqlStatement> GetStatements(StatementList statementList) => statementList.Statements.Where(FilterStatement);

        private static bool FilterStatement(TSqlStatement statement) => !ExcludedTypes.Any(statement.GetType().IsAssignableFrom);
    }
}