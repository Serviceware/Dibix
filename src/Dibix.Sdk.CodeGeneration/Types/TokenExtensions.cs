using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class TokenExtensions
    {
        public static Token<T> ToToken<T>(this JToken json, T value, string source)
        {
            if (json == null)
                return null;

            JsonSourceInfo sourceInfo = json.GetSourceInfo();
            return new Token<T>(value, source, sourceInfo.LineNumber, sourceInfo.LinePosition);
        }
    }
}