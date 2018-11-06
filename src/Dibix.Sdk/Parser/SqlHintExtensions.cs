using System;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    internal static class SqlHintExtensions
    {
        public static string SingleHint(this TSqlFragment fragment, string hintType, int startIndex = 0)
        {
            return SqlHintReader.Read(fragment, startIndex)
                                .Where(x => x.Kind == hintType)
                                .Select(x => x.Value)
                                .FirstOrDefault();
        }

        public static bool IsSet(this TSqlFragment fragment, string hintType, int startIndex = 0)
        {
            return SqlHintReader.Read(fragment, startIndex).Any(x => x.Kind == hintType);
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
    }
}
