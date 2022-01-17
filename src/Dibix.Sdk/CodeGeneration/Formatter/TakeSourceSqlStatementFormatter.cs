using System;
using System.Data;
using System.Linq;
using System.Text;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class TakeSourceSqlStatementFormatter : SqlStatementFormatter, ISqlStatementFormatter
    {
        protected override FormattedSqlStatement Format(SqlStatementDefinition statementDefinition, StatementList statementList)
        {
            StringBuilder sb = new StringBuilder();

            void StatementHandler(TSqlStatement statement, int statementIndex, int statementCount)
            {
                if (statementIndex + 1 < statementCount)
                {
                    if (base.StripWhiteSpace)
                        sb.Append(';');
                    else
                        sb.AppendLine()
                          .AppendLine();
                }
            }

            void TokenHandler(TSqlParserToken token)
            {
                if (token.TokenType == SqlTokenType.SingleLineComment || token.TokenType == SqlTokenType.MultilineComment) 
                    return;

                sb.Append(token.Text);
            }

            base.CollectStatements(statementList, StatementHandler, TokenHandler);

            string content = DecreaseIndentation(sb.ToString());
            return new FormattedSqlStatement(content, CommandType.Text);
        }

        private static string DecreaseIndentation(string text) => String.Join("\n", text.Split('\n').Select((line, lineIndex) =>
        {
            if (lineIndex == 0)
                return line;

            int index = line.Select((@char, charIndex) => new { Char = @char, CharIndex = charIndex })
                            .Where(y => y.CharIndex >= Indentation || y.Char != ' ')
                            .Select(y => y.CharIndex)
                            .FirstOrDefault();

            return line.Substring(index);
        }));
    }
}