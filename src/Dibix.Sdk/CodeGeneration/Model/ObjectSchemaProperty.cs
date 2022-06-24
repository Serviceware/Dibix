using System;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ObjectSchemaProperty
    {
        public Token<string> Name { get; }
        public TypeReference Type { get; }
        public ValueReference DefaultValue { get; }
        public DateTimeKind DateTimeKind { get; }
        public SerializationBehavior SerializationBehavior { get; }
        public bool IsPartOfKey { get; }
        public bool IsOptional { get; }
        public bool IsDiscriminator { get; }
        public bool IsObfuscated { get; }
        public bool IsRelativeHttpsUrl { get; }

        public ObjectSchemaProperty(Token<string> name, TypeReference type, ValueReference defaultValue = default, SerializationBehavior serializationBehavior = default, DateTimeKind dateTimeKind = default, bool isPartOfKey = default, bool isOptional = default, bool isDiscriminator = default, bool isObfuscated = default, bool isRelativeHttpsUrl = default)
        {
            this.Name = name;
            this.Type = type;
            this.DefaultValue = defaultValue;
            this.DateTimeKind = dateTimeKind;
            this.SerializationBehavior = !isDiscriminator ? serializationBehavior : SerializationBehavior.Never;
            this.IsPartOfKey = isPartOfKey;
            this.IsOptional = isOptional;
            this.IsDiscriminator = isDiscriminator;
            this.IsObfuscated = isObfuscated;
            this.IsRelativeHttpsUrl = isRelativeHttpsUrl;
        }
    }
}