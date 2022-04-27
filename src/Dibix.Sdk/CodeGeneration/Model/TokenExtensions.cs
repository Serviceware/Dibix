using Dibix.Sdk.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class TokenExtensions
    {
        public static Token<T> ToToken<T>(this JToken json, T value, string source)
        {
            if (json == null)
                return null;

            IJsonLineInfo lineInfo = json.GetLineInfo();
            return new Token<T>(value, source, lineInfo.LineNumber, lineInfo.LinePosition);
        }
    }
}