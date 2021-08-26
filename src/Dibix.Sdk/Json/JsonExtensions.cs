using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk.Json
{
    internal static class JsonExtensions
    {
        public static JToken LoadJson(string jsonFilePath)
        {
            using (Stream stream = File.OpenRead(jsonFilePath))
            {
                using (TextReader textReader = new StreamReader(stream))
                {
                    using (JsonReader jsonReader = new JsonTextReader(textReader))
                    {
                        return JToken.Load(jsonReader);
                    }
                }
            }
        }

        public static IJsonLineInfo GetLineInfo(this JToken token)
        {
            IJsonLineInfo lineInfo = token;

            int? linePosition = null;
            if (lineInfo.HasLineInfo())
            {
                switch (token)
                {
                    case JValue value:
                        linePosition = value.GetCorrectLinePosition();
                        break;

                    case JProperty property:
                        linePosition = property.GetCorrectLinePosition();
                        break;
                }
            }

            if (!linePosition.HasValue)
                return lineInfo;

            return new JsonLineInfo(lineInfo.LineNumber, linePosition.Value);
        }

        // The line positions are somewhat weird and unexpected
        // Not sure if this is a bug, but we have to adjust the position to get the actual start of the value
        private static int GetCorrectLinePosition(this JValue value)
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
        private static int GetCorrectLinePosition(this JProperty property)
        {
            IJsonLineInfo lineInfo = property;
            int result = lineInfo.LinePosition - 1 - property.Name.Length;
            return result;
        }

        private sealed class JsonLineInfo : IJsonLineInfo
        {
            public int LineNumber { get; }
            public int LinePosition { get; }

            public JsonLineInfo(int lineNumber, int linePosition)
            {
                this.LineNumber = lineNumber;
                this.LinePosition = linePosition;
            }

            public bool HasLineInfo() => true;
        }
    }
}