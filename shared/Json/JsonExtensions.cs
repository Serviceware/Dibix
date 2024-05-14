using System;
using System.IO;
using System.Text;
using Dibix.Sdk.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk
{
    internal static class JsonExtensions
    {
        public static JProperty GetPropertySafe(this JObject @object, string propertyName)
        {
            JProperty property = @object.Property(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing property '{propertyName}' at {@object.Path}");

            return property;
        }

        public static void SetFileSource(this JToken json, string filePath)
        {
            json.AddAnnotation(new JsonFileSourceAnnotation(filePath));
        }
        public static void SetFileSource(this JContainer json, string filePath)
        {
            foreach (JToken token in json.DescendantsAndSelf())
                SetFileSource(token, filePath);
        }

        public static SourceLocation GetSourceInfo(this JToken token)
        {
            string filePath = (token.Annotation<JsonFileSourceAnnotation>() ?? token.Root.Annotation<JsonFileSourceAnnotation>())?.FilePath;

            IJsonLineInfo lineInfo = token;

            bool hasLineInfo = lineInfo.HasLineInfo();
            int lineNumber = lineInfo.LineNumber;
            int linePosition = lineInfo.LinePosition;

            if (hasLineInfo)
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

            return new SourceLocation(filePath, lineNumber, linePosition);
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
    }
}