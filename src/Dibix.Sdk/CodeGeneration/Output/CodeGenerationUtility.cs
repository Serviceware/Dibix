using System.Text.RegularExpressions;

namespace Dibix.Sdk.CodeGeneration
{
    public static class CodeGenerationUtility
    {
        public static string FormatCommandText(string commandText, CommandTextFormatting formatting)
        {
            if (commandText == null)
                return null;

            string formatted = commandText.Trim();

            if (formatting.HasFlag(CommandTextFormatting.WhiteStripped))
                formatted = Regex.Replace(formatted, @"\s+", " ");

            if (formatting.HasFlag(CommandTextFormatting.StripDoubleQuotes))
                formatted = formatted.Replace("\"", formatting.HasFlag(CommandTextFormatting.Verbatim) ? "\"\"" : "\\\"");

            if (formatting.HasFlag(CommandTextFormatting.Minified))
                formatted = formatted.Replace("\r\n", @"\r\n");

            return formatted;
        }
    }
}