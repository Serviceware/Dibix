using System;

namespace Dibix.Testing
{
    internal static class StringExtensions
    {
        public static string NormalizeLineEndings(this string str)
        {
            return str.Replace("\r\n", "\n")
                      .Replace("\r", "\n")
                      .Replace("\n", Environment.NewLine)
                      .Replace("\\r\\n", "\\n")
                      .Replace("\\r", "\\n")
                      .Replace("\\n", Environment.NewLine.Replace("\r", "\\r")
                                                         .Replace("\n", "\\n"));
        }
    }
}