using System;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk
{
    internal static class JsonSchemaDefinition
    {
        private static readonly ConcurrentDictionary<string, JSchema> Schemas = new ConcurrentDictionary<string, JSchema>();

        public static JSchema GetSchema(string @namespace, string schema) => Schemas.GetOrAdd(schema, x => LoadSchema(@namespace, x));

        private static JSchema LoadSchema(string @namespace, string schemaName)
        {
            Type type = typeof(JsonSchemaDefinition);
            string resourcePath = $"{@namespace}.{schemaName}.json";
            using (Stream stream = type.Assembly.GetManifestResourceStream(resourcePath))
            {
                using (TextReader textReader = new StreamReader(stream))
                {
                    using (JsonReader jsonReader = new JsonTextReader(textReader))
                    {
                        return JSchema.Load(jsonReader);
                    }
                }
            }
        }
    }
}