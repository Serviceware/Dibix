using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Dibix
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

        public static string ToPascalCase(this string text)
        {
            string pascalCase = ToCamelOrPascalCase(text, Char.ToUpperInvariant);
            return pascalCase;
        }

        public static string ToCamelCase(this string text)
        {
            string camelCase = ToCamelOrPascalCase(text, Char.ToLowerInvariant);
            return camelCase;
        }
        public static string ToCamelCase(this IEnumerable<string> words)
        {
            StringBuilder sb = new StringBuilder();

            using IEnumerator<string> enumerator = words.GetEnumerator();
            if (enumerator.MoveNext())
                sb.Append(enumerator.Current.ToCamelCase());

            while (enumerator.MoveNext())
                sb.Append(enumerator.Current.ToPascalCase());

            string camelCase = sb.ToString();
            return camelCase;
        }

        private static string ToCamelOrPascalCase(string text, Func<char, char> firstLetterTransformer)
        {
            StringBuilder sb = new StringBuilder();
            if (text.Length > 0)
                sb.Append(firstLetterTransformer(text[0]));

            if (text.Length > 1)
                sb.Append(text.Substring(1));

            string camelCase = sb.ToString();
            return camelCase;
        }
    }
}