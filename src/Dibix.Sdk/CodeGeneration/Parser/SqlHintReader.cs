using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlHintReader
    {
        private static readonly Regex ParseCommentRegex = new Regex(@"^(?:\/\*|--) ?@([\w]+)(?: (?:([#\w.[\],;?]+)|([#\w:.[\],;? ]+)))?(?: ?\*\/)?$", RegexOptions.Compiled);

        public static IEnumerable<SqlHint> Read(params KeyValuePair<int, string>[] lines) => Read(lines.AsEnumerable());
        public static IEnumerable<SqlHint> Read(IEnumerable<KeyValuePair<int, string>> lines)
        {
            foreach (KeyValuePair<int, string> line in lines)
            {
                string text = line.Value.Trim();
                if (String.IsNullOrEmpty(text))
                    continue;

                Match match = ParseCommentRegex.Match(text);
                if (!match.Success)
                    yield break;

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

                SqlHint hint = new SqlHint(keyGroup.Value, line.Key, column);
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