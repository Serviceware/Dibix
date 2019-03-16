using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlHintReader
    {
        private static readonly Regex ParseCommentRegex = new Regex(@"^(?:\/\*|--) ?@([\w]+)(?: (?:([\w.[\],;?]+)|([\w:.[\],;? ]+)))?(?: ?\*\/)?$", RegexOptions.Compiled);

        public static IEnumerable<SqlHint> Read(TSqlFragment fragment, int startIndex = 0)
        {
            for (int i = startIndex; i < fragment.FirstTokenIndex; i++)
            {
                TSqlParserToken token = fragment.ScriptTokenStream[i];
                if (token.TokenType != TSqlTokenType.SingleLineComment && token.TokenType != TSqlTokenType.MultilineComment)
                    continue;

                Match match = ParseCommentRegex.Match(token.Text.Trim());
                if (!match.Success)
                    continue;

                Group keyGroup = match.Groups[1];
                Group singleValueGroup = match.Groups[2];
                Group multiValueGroup = match.Groups[3];
                int column = 1;
                if (singleValueGroup.Success)
                    column += singleValueGroup.Index;
                else if (multiValueGroup.Success)
                    column += multiValueGroup.Index;
                else
                    column += keyGroup.Index;

                SqlHint hint = new SqlHint(keyGroup.Value, token.Line, column);
                if (singleValueGroup.Success)
                {
                    hint.Properties.Add(SqlHint.Default, singleValueGroup.Value);
                }
                else if (multiValueGroup.Success)
                {
                    var properties = multiValueGroup.Value
                                                    .Split(' ')
                                                    .Select(x =>
                                                    {
                                                        string[] parts = x.Split(':');
                                                        bool hasPropertyName = parts.Length > 1;
                                                        string key = hasPropertyName ? parts[0] : SqlHint.Default;
                                                        string value = parts[hasPropertyName ? 1 : 0];
                                                        return new KeyValuePair<string, string>(key, value);
                                                    });

                    hint.Properties.AddRange(properties);
                }

                yield return hint;
            }
        }
    }
}