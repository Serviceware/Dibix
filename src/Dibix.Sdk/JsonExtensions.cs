using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk
{
    internal static class JsonExtensions
    {
        public static IEnumerable<string> GetPropertyValues(this JObject json, string propertyName)
        {
            JProperty property = json.Property(propertyName);
            if (property == null)
                return Enumerable.Empty<string>();

            JToken value = property.Value;
            switch (value.Type)
            {
                case JTokenType.String: return Enumerable.Repeat(value.Value<string>(), 1);
                case JTokenType.Array: return value.Values<string>();
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public static IEnumerable<JObject> GetObjects(this JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object: return Enumerable.Repeat(token.Value<JObject>(), 1);
                case JTokenType.Array: return token.Values<JObject>();
                case JTokenType.Null: return Enumerable.Empty<JObject>();
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
