using System;
using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Dibix.Http.Host
{
    internal sealed class JsonPostConfigureOptions : IPostConfigureOptions<JsonOptions>
    {
        public void PostConfigure(string? name, JsonOptions options) => options.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { AppendShouldSerializeMethod } };

        private static void AppendShouldSerializeMethod(JsonTypeInfo typeInfo)
        {
            foreach (JsonPropertyInfo property in typeInfo.Properties)
            {
                if (property.AttributeProvider == null)
                    continue;

                if (!property.AttributeProvider.IsDefined(typeof(IgnoreSerializationIfEmptyAttribute), inherit: false))
                    continue;

                property.ShouldSerialize = (_, memberValue) => memberValue is not IEnumerable enumerable || HasItems(enumerable);
            }
        }

        private static bool HasItems(IEnumerable enumerable)
        {
            IEnumerator enumerator = enumerable.GetEnumerator();
            return enumerator.MoveNext();
        }
    }
}