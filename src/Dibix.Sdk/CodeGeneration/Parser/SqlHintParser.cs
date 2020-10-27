using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlHintParser
    {
        private static readonly Regex ParseCommentRegex = new Regex(@"^(?:\/\*|--) ?@([\w]+)(?: (?:([#\w.[\],;?]+)|([#\w:.[\],;? ]+)))?(?: ?\*\/)?$", RegexOptions.Compiled);
        private readonly string _source;
        private readonly ILogger _logger;

        private SqlHintParser(string source, ILogger logger)
        {
            this._source = source;
            this._logger = logger;
        }

        public static IEnumerable<SqlHint> ReadHeader(string source, ILogger logger, TSqlFragment fragment)
        {
            SqlHintParser parser = new SqlHintParser(source, logger);

            foreach (TSqlParserToken token in fragment.AsEnumerable())
            {
                if (token.TokenType == TSqlTokenType.WhiteSpace)
                    continue;

                // Since we are reading the header, stop processing, once any non-comment T-SQL token occurs
                bool isComment = token.TokenType == TSqlTokenType.MultilineComment || TSqlTokenType.SingleLineComment == token.TokenType;
                if (!isComment)
                    yield break;

                IEnumerable<SqlHint> hints = parser.Read(token);
                foreach (SqlHint hint in hints)
                {
                    yield return hint;
                }
            }
        }

        public static IEnumerable<SqlHint> ReadFragment(string source, ILogger logger, TSqlFragment fragment)
        {
            int startIndex = fragment.FirstTokenIndex;
            TSqlParserToken token;
            do
            {
                token = fragment.ScriptTokenStream[--startIndex];
            } while (token.TokenType == TSqlTokenType.WhiteSpace);

            SqlHintParser parser = new SqlHintParser(source, logger);
            return parser.Read(token);
        }

        private IEnumerable<SqlHint> Read(TSqlParserToken token)
        {
            MatchCollection matches = ParseCommentRegex.Matches(token.Text);
            if (matches.Count == 0)
                yield break;

            foreach (Match match in matches)
            {
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
                    hint.Properties.Add(SqlHint.DefaultProperty, singleValueGroup.Value);
                }
                else if (multiValueGroup.Success)
                {
                    foreach (string property in multiValueGroup.Value.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] parts = property.Split(':');
                        bool hasPropertyName = parts.Length > 1;
                        string key = hasPropertyName ? parts[0] : SqlHint.DefaultProperty;
                        if (hint.Properties.ContainsKey(key))
                        {
                            string errorMessage = key != SqlHint.DefaultProperty ? $"Duplicate property for @{hint.Kind}.{key}" : $"Multiple default properties specified for @{hint.Kind}";
                            this._logger.LogError(null, errorMessage, this._source, token.Line, multiValueGroup.Index + 1);
                            yield break;
                        }

                        string value = parts[hasPropertyName ? 1 : 0];
                        hint.Properties.Add(key, value);
                    }
                }

                yield return hint;
            }
        }
    }
}