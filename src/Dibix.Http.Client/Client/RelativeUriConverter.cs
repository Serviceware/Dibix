using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Http.Client
{
    internal sealed class RelativeUriConverter : JsonConverter
    {
        private readonly string _hostName;

        public override bool CanRead => true;
        public override bool CanWrite => false;

        public RelativeUriConverter(string hostName) => this._hostName = hostName;

        public override bool CanConvert(Type objectType) => objectType == typeof(Uri);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotSupportedException();

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JToken json = JToken.Load(reader);
            Uri uri = new Uri(new Uri($"https://{this._hostName}"), (string)json);
            return uri;
        }
    }
}