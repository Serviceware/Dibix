using System;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ObjectSchemaProperty
    {
        public string Name { get; }
        public TypeReference Type { get; }
        public bool IsPartOfKey { get; }
        public bool IsOptional { get; }
        public bool IsDiscriminator { get; }
        public DefaultValue DefaultValue { get; internal set; }
        public DateTimeKind DateTimeKind { get; }
        public bool Obfuscated { get; }
        public SerializationBehavior SerializationBehavior { get; }

        public ObjectSchemaProperty(string name, TypeReference type = default, bool isPartOfKey = default, bool isOptional = default, bool isDiscriminator = default, DefaultValue defaultValue = default, SerializationBehavior serializationBehavior = default, DateTimeKind dateTimeKind = default, bool obfuscated = default)
        {
            this.Name = name;
            this.Type = type;
            this.IsPartOfKey = isPartOfKey;
            this.IsOptional = isOptional;
            this.IsDiscriminator = isDiscriminator;
            this.DefaultValue = defaultValue;
            this.DateTimeKind = dateTimeKind;
            this.Obfuscated = obfuscated;
            this.SerializationBehavior = !isDiscriminator ? serializationBehavior : SerializationBehavior.Never;
        }
    }
}