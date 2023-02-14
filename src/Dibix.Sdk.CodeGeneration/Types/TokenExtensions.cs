using Dibix.Sdk.Abstractions;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class TokenExtensions
    {
        public static Token<T> ToToken<T>(this JToken json, T value)
        {
            if (json == null)
                return null;

            SourceLocation sourceInfo = json.GetSourceInfo();
            return new Token<T>(value, sourceInfo);
        }
    }
}