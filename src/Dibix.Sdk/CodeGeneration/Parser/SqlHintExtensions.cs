using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class SqlHintExtensions
    {
        public static SqlHint SingleHint(this IEnumerable<SqlHint> hints, string hintType) => hints.FirstOrDefault(x => x.Kind == hintType);

        public static string SingleHintValue(this IEnumerable<SqlHint> hints, string hintType)
        {
            return hints.Where(x => x.Kind == hintType)
                        .Select(x => x.Value)
                        .FirstOrDefault();
        }

        public static bool IsSet(this IEnumerable<SqlHint> hints, string hintType)
        {
            return hints.Any(x => x.Kind == hintType);
        }

        public static bool TrySelectValueOrContent(this SqlHint hint, string key, Action<string> errorHandler, out string result)
        {
            if (hint.Properties.TryGetValue(key, out result))
                return true;

            if (hint.Properties.TryGetValue(SqlHint.DefaultProperty, out result))
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
