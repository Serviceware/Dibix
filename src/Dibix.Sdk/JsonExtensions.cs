using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Dibix.Sdk
{
    internal static class JsonExtensions
    {
        public static IEnumerable<string> GetValues(this JProperty property)
        {
            switch (property.Value.Type)
            {
                case JTokenType.String: return property.Values<string>();
                case JTokenType.Array: return property.Value.Values<string>();
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
