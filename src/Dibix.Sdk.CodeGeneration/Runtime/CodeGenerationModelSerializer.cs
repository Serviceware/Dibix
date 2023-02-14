using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class CodeGenerationModelSerializer
    {
        private static readonly IDictionary<Assembly, IPersistedCodeGenerationModel> Map = new Dictionary<Assembly, IPersistedCodeGenerationModel>();

        public static IPersistedCodeGenerationModel Read(Assembly assembly)
        {
            if (!Map.TryGetValue(assembly, out IPersistedCodeGenerationModel model))
            {
                model = LoadModelFromResource(assembly);
                Map.Add(assembly, model);
            }

            return model;
        }

        public static void Write(IPersistedCodeGenerationModel model, string path)
        {
            string serializedModelJson = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Objects,
                ContractResolver = new WriteModelJsonContractResolver(),
                Converters =
                {
                    new StringEnumConverter(),
                    new SchemaCollectionConverter()
                }
            });
            File.WriteAllText(path, serializedModelJson);
        }

        private static IPersistedCodeGenerationModel LoadModelFromResource(Assembly assembly)
        {
            using (Stream stream = assembly.GetManifestResourceStream("Model"))
            {
                if (stream == null)
                {
                    // Leaf projects do not embed their model, because nesting endpoint projects is not supported.
                    // In some cases, DDL projects might already contain endpoints. And that DDL might be referenced by another DML project.
                    // This is fine, and therefore we just return an empty model instead of throwing an exception.
                    // An empty model also ensures, that no endpoint can target an artifact from a referenced leaf project.
                    //throw new InvalidOperationException($"Model resource not found in assembly: {assembly}");
                    return new CodeGenerationModel();
                }

                using (TextReader textReader = new StreamReader(stream))
                {
                    using (JsonReader jsonReader = new JsonTextReader(textReader))
                    {
                        JsonSerializer serializer = new JsonSerializer
                        {
                            TypeNameHandling = TypeNameHandling.Objects,
                            Converters = { new SchemaDefinitionSourceConverter() }
                        };
                        IPersistedCodeGenerationModel model = serializer.Deserialize<CodeGenerationModel>(jsonReader);
                        if (model == null)
                            throw new InvalidOperationException($"Model is empty: {assembly}");

                        return model;
                    }
                }
            }
        }

        private sealed class SchemaDefinitionSourceConverter : JsonConverter
        {
            public override bool CanRead => true;
            public override bool CanWrite => false;

            public override bool CanConvert(Type objectType) => objectType == typeof(SchemaDefinitionSource);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotSupportedException();

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (!(reader.Value is string value))
                    return null;

                SchemaDefinitionSource source = (SchemaDefinitionSource)Enum.Parse(typeof(SchemaDefinitionSource), value);

                // Since this model is loaded externally, the source that once was 'Local' is now 'Foreign'
                return SchemaDefinitionSource.Local.HasFlag(source) ? SchemaDefinitionSource.Foreign : source;
            }
        }

        private sealed class WriteModelJsonContractResolver : DefaultContractResolver
        {
            private static readonly MemberInfo[] IgnoredMembers =
            {
                typeof(SchemaDefinition).GetProperty(nameof(SchemaDefinition.ExternalSchemaInfo))
            };

            // Preserve value type when serializing 'object'
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                if (IgnoredMembers.Contains(member))
                    return null;

                JsonProperty property = base.CreateProperty(member, memberSerialization);

                if (member.DeclaringType == typeof(PrimitiveValueReference) && member.Name == nameof(PrimitiveValueReference.Value))
                {
                    property.Converter = new DefaultValueConverter();
                }

                return property;
            }

            // Only serialize CodeGenerationModel members from IPersistedCodeGenerationModel
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                if (type == typeof(CodeGenerationModel))
                    return base.CreateProperties(typeof(IPersistedCodeGenerationModel), memberSerialization);

                IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
                return properties;
            }
        }

        // Only serialize local schemas
        private sealed class SchemaCollectionConverter : JsonConverter
        {
            public override bool CanRead => false;
            public override bool CanWrite => true;

            public override bool CanConvert(Type objectType) => objectType == typeof(Collection<SchemaDefinition>);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                ICollection<SchemaDefinition> schemaDefinitions = ((ICollection<SchemaDefinition>)value).Where(x => SchemaDefinitionSource.Local.HasFlag(x.Source)).ToArray();
                serializer.Serialize(writer, schemaDefinitions);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotSupportedException();
        }

        // Preserve value type when serializing 'object'
        private sealed class DefaultValueConverter : JsonConverter
        {
            public override bool CanRead => false;
            public override bool CanWrite => true;
            public override bool CanConvert(Type objectType) => objectType == typeof(object);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotSupportedException();

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (serializer.TypeNameHandling.HasFlag(TypeNameHandling.Objects))
                {
                    Type type = value.GetType();

                    writer.WriteStartObject();
                    writer.WritePropertyName("$type", false);

                    switch (serializer.TypeNameAssemblyFormatHandling)
                    {
                        case TypeNameAssemblyFormatHandling.Full:
                            writer.WriteValue(type.AssemblyQualifiedName);
                            break;

                        default:
                            writer.WriteValue(type.FullName);
                            break;
                    }

                    writer.WritePropertyName("$value", false);
                    writer.WriteValue(value);
                    writer.WriteEndObject();
                }
                else
                {
                    writer.WriteValue(value);
                }
            }
        }
    }
}