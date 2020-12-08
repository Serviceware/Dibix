using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.Json
{
    internal static class JsonExtensions
    {
        // The line positions are somewhat weird and unexpected
        // Not sure if this is a bug, but we have to adjust the position to get the actual start of the value
        public static int GetCorrectLinePosition(this JValue value)
        {
            IJsonLineInfo lineInfo = value;
            StringBuilder sb = new StringBuilder();
            using (TextWriter textWriter = new StringWriter(sb))
            {
                using (JsonWriter jsonWriter = new JsonTextWriter(textWriter))
                {
                    value.WriteTo(jsonWriter);
                    int valueEnd = lineInfo.LinePosition + 1;
                    int result = valueEnd - sb.Length;

                    // And while we're at it anyways, we can skip ahead the " just for convenience
                    if (value.Type == JTokenType.String)
                        result++;

                    return result;
                }
            }
        }
        public static int GetCorrectLinePosition(this JProperty property)
        {
            IJsonLineInfo lineInfo = property;
            int result = lineInfo.LinePosition - 1 - property.Name.Length;
            return result;
        }
    }
}