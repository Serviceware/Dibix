using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlHintReader
    {
        private static readonly Regex ParseCommentRegex = new Regex(@"^(?:\/\*|--) ?@([\w]+)(?: (?:([#\w.[\],;?]+)|([#\w:.[\],;? ]+)))?(?: ?\*\/)?$", RegexOptions.Compiled);

        public static IEnumerable<SqlHint> Read(params KeyValuePair<int, string>[] tokens) => Read(tokens.AsEnumerable());
        public static IEnumerable<SqlHint> Read(IEnumerable<KeyValuePair<int, string>> tokens)
        {
            foreach (KeyValuePair<int, string> token in tokens)
            {
                string text = (token.Value ?? String.Empty).Trim();

                // Skip whitespace
                if (String.IsNullOrEmpty(text))
                    continue;

                Match match = ParseCommentRegex.Match(text);
                if (!match.Success)
                {
                    // Skip non dibix comments but keep reading
                    // i.E.:
                    // -- DROP procedure
                    // -- @Name procedure
                    if (text.StartsWith("--", StringComparison.Ordinal))
                        continue;

                    // We couldn't find a dibix hint at the header of the file before actual content starts
                    yield break;
                }

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

                SqlHint hint = new SqlHint(keyGroup.Value, token.Key, column);
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