using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlHintExtensions
    {
        public static IEnumerable<SqlHint> Hints(this TSqlFragment fragment)
        {
            var lines = fragment.AsEnumerable()
                                .Where(x => x.TokenType == TSqlTokenType.SingleLineComment || x.TokenType == TSqlTokenType.MultilineComment)
                                .Select(x => new KeyValuePair<int, string>(x.Line, x.Text));

            return SqlHintReader.Read(lines);
        }

        public static SqlHint SingleHint(this IEnumerable<SqlHint> hints, string hintType) => hints.FirstOrDefault(x => x.Kind == hintType);

        public static string SingleHintValue(this TSqlParserToken token, string hintType)
        {
            return Hints(token).SingleHintValue(hintType);
        }
        public static string SingleHintValue(this IEnumerable<SqlHint> hints, string hintType)
        {
            return hints.Where(x => x.Kind == hintType)
                        .Select(x => x.Value)
                        .FirstOrDefault();
        }

        public static bool IsSet(this TSqlParserToken token, string hintType)
        {
            return Hints(token).Any(x => x.Kind == hintType);
        }

        public static bool TrySelectValueOrContent(this SqlHint hint, string key, Action<string> errorHandler, out string result)
        {
            if (hint.Properties.TryGetValue(key, out result))
                return true;

            if (hint.Properties.TryGetValue(SqlHint.Default, out result))
                return true;

            errorHandler($"Missing property '{key}'");
            return false;
        }

        public static string SelectValueOrDefault(this SqlHint hint, string key) { return SelectValueOrDefault(hint, key, x => x); }
        public static TValue SelectValueOrDefault<TValue>(this SqlHint hint, string key, Func<string, TValue> converter)
        {
            return hint.Properties.TryGetValue(key, out var value) ? converter(value) : default;
        }

        private static IEnumerable<SqlHint> Hints(this TSqlParserToken token)
        {
            return SqlHintReader.Read(new KeyValuePair<int, string>(token.Line, token.Text));
        }
    }
}
