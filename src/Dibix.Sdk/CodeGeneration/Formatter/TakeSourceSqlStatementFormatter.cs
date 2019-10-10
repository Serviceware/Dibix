using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class TakeSourceSqlStatementFormatter : SqlStatementFormatter, ISqlStatementFormatter
    {
        public override string Format(SqlStatementInfo info, StatementList body)
        {
            StringBuilder sb = new StringBuilder();
            IList<TSqlStatement> statements = base.GetStatements(body).ToArray();
            for (int i = 0; i < statements.Count; i++)
            {
                TSqlStatement statement = statements[i];
                int startIndex = statement.FirstTokenIndex;
                int endIndex = statement.LastTokenIndex;
                for (int j = startIndex; j <= endIndex; j++)
                {
                    TSqlParserToken token = body.ScriptTokenStream[j];
                    if (token.TokenType == SqlTokenType.SingleLineComment || token.TokenType == SqlTokenType.MultilineComment)
                        continue;

                    sb.Append(token.Text);
                }

                if (i + 1 < statements.Count)
                    sb.AppendLine()
                      .AppendLine();
            }
            return DecreaseIndentation(sb.ToString());
        }

        private static string DecreaseIndentation(string text)
        {
            const int indentation = 4;
            text = text.Replace("\t", new string(' ', indentation));
            return String.Join("\n", text.Split('\n').Select((line, lineIndex) =>
            {
                if (lineIndex == 0)
                    return line;

                int index = line.Select((@char, charIndex) => new { Char = @char, CharIndex = charIndex })
                                .Where(y => y.CharIndex >= indentation || y.Char != ' ')
                                .Select(y => y.CharIndex)
                                .FirstOrDefault();

                return line.Substring(index);
            }));
        }
    }
}