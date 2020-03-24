namespace Dibix.Sdk
{
    public static class CanonicalLogFormat
    {
        public static string ToErrorString(string code, string text, string source, int line, int column) => ToString("Error", code, text, source, line, column);
        private static string ToString(string category, string code, string text, string source, int line, int column) => $"{source}({line},{column}) : {category} {code}: {text}";
    }
}