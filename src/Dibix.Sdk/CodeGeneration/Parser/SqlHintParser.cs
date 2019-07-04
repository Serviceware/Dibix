﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class SqlHintParser
    {
        private static readonly Regex ParseCommentRegex = new Regex(@"^(?:\/\*|--) ?@([\w]+)(?: (?:([#\w.[\],;?]+)|([#\w:.[\],;? ]+)))?(?: ?\*\/)?$", RegexOptions.Compiled);
        private readonly string _source;
        private readonly IErrorReporter _errorReporter;

        private SqlHintParser(string source, IErrorReporter errorReporter)
        {
            this._source = source;
            this._errorReporter = errorReporter;
        }

        public static IEnumerable<SqlHint> FromFragment(string source, IErrorReporter errorReporter, TSqlFragment fragment)
        {
            SqlHintParser parser = new SqlHintParser(source, errorReporter);
            return parser.Read(fragment.AsEnumerable().Select(x => new SqlHintToken(x.Line, x.Text)));
        }

        public static IEnumerable<SqlHint> FromToken(string source, IErrorReporter errorReporter, TSqlParserToken token)
        {
            SqlHintParser parser = new SqlHintParser(source, errorReporter);
            return parser.Read(Enumerable.Repeat(new SqlHintToken(token.Line, token.Text), 1));
        }

        public static IEnumerable<SqlHint> FromFile(string filePath, IErrorReporter errorReporter)
        {
            SqlHintParser parser = new SqlHintParser(filePath, errorReporter);
            return parser.Read(File.ReadLines(filePath).Select((x, i) => new SqlHintToken(i + 1, x)));
        }

        private IEnumerable<SqlHint> Read(IEnumerable<SqlHintToken> tokens)
        {
            foreach (SqlHintToken token in tokens)
            {
                string text = (token.Text ?? String.Empty).Trim();

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

                SqlHint hint = new SqlHint(keyGroup.Value, token.Line, column);
                if (singleValueGroup.Success)
                {
                    hint.Properties.Add(SqlHint.Default, singleValueGroup.Value);
                }
                else if (multiValueGroup.Success)
                {
                    foreach (string property in multiValueGroup.Value.Split(' '))
                    {
                        string[] parts = property.Split(':');
                        bool hasPropertyName = parts.Length > 1;
                        string key = hasPropertyName ? parts[0] : SqlHint.Default;
                        if (hint.Properties.ContainsKey(key))
                        {
                            string errorMessage = key != SqlHint.Default ? $"Duplicate property for @{hint.Kind}.{key}" : $"Multiple default properties specified for @{hint.Kind}";
                            this._errorReporter.RegisterError(this._source, token.Line, multiValueGroup.Index + 1, null, errorMessage);
                            yield break;
                        }

                        string value = parts[hasPropertyName ? 1 : 0];
                        hint.Properties.Add(key, value);
                    }
                }

                yield return hint;
            }
        }

        private sealed class SqlHintToken
        {
            public int Line { get; }
            public string Text { get; }

            public SqlHintToken(int line, string text)
            {
                this.Line = line;
                this.Text = text;
            }
        }
    }
}