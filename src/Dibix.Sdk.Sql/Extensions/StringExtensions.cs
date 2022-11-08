using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Dibix.Sdk.Sql
{
    internal static class StringExtensions
    {
        public static IEnumerable<string> SplitWords(this string input)
        {
            string[] parts = Regex.Split(input, "(?<!^)(?=[A-Z])");
            for (int i = 1; i < parts.Length; i++)
                parts[i] = parts[i].ToLowerInvariant();

            return parts;
        }
    }
}