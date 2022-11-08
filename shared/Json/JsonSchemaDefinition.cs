using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace Dibix.Sdk
{
    internal static class JsonSchemaDefinition
    {
        private static readonly ConcurrentDictionary<string, JSchema> Schemas = new ConcurrentDictionary<string, JSchema>();

        public static JSchema GetSchema(Assembly assembly, string schemaFileName) => Schemas.GetOrAdd(schemaFileName, x => LoadSchema(assembly, $"{x}.json"));

        private static JSchema LoadSchema(Assembly assembly, string resourcePath)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException(@$"Could not find embedded resource '{resourcePath}' in assembly '{assembly}'
Location: {assembly.Location}");
                }

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