using System;
using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class StringExtensions
    {
        public static string ToCamelCase(string text)
        {
            StringBuilder sb = new StringBuilder();
            if (text.Length > 0)
                sb.Append(Char.ToLowerInvariant(text[0]));

            if (text.Length > 1)
                sb.Append(text.Substring(1));

            string camelCase = sb.ToString();
            return camelCase;
        }
    }
}