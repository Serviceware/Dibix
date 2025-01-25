using System;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ObjectSchemaProperty : IPropertyDescriptor
    {
        private readonly Lazy<TypeReference> _typeResolver;
        private readonly Lazy<ValueReference> _defaultValueResolver;

        public Token<string> Name { get; }
        public TypeReference Type => _typeResolver.Value;
        public ValueReference DefaultValue => _defaultValueResolver.Value;
        public DateTimeKind DateTimeKind { get; }
        public SerializationBehavior SerializationBehavior { get; }
        public bool IsPartOfKey { get; }
        public bool IsOptional { get; }
        public bool IsDiscriminator { get; }
        public bool IsObfuscated { get; }
        public bool IsRelativeHttpsUrl { get; }
        public long? MaxLength { get; }
        public byte? Precision { get; }
        public byte? Scale { get; }
        string IPropertyDescriptor.Name => Name;

        public ObjectSchemaProperty(Token<string> name, TypeReference type, ValueReference defaultValue = default, SerializationBehavior serializationBehavior = default, DateTimeKind dateTimeKind = default, bool isPartOfKey = default, bool isOptional = default, bool isDiscriminator = default, bool isObfuscated = default, bool isRelativeHttpsUrl = default, long? maxLength = default, byte? precision = default, byte? scale = default) : this(name, () => type, _ => defaultValue, serializationBehavior, dateTimeKind, isPartOfKey, isOptional, isDiscriminator, isObfuscated, isRelativeHttpsUrl, maxLength, precision, scale) { }
        internal ObjectSchemaProperty(Token<string> name, Func<TypeReference> typeResolver, Func<TypeReference, ValueReference> defaultValueResolver, SerializationBehavior serializationBehavior, DateTimeKind dateTimeKind, bool isPartOfKey, bool isOptional, bool isDiscriminator, bool isObfuscated, bool isRelativeHttpsUrl, long? maxLength = default, byte? precision = default, byte? scale = default)
        {
            _typeResolver = new Lazy<TypeReference>(typeResolver);
            _defaultValueResolver = new Lazy<ValueReference>(() => defaultValueResolver(Type));
            Name = name;
            DateTimeKind = dateTimeKind;
            SerializationBehavior = !isDiscriminator ? serializationBehavior : SerializationBehavior.Never;
            IsPartOfKey = isPartOfKey;
            IsOptional = isOptional;
            IsDiscriminator = isDiscriminator;
            IsObfuscated = isObfuscated;
            IsRelativeHttpsUrl = isRelativeHttpsUrl;
            MaxLength = maxLength;
            Precision = precision;
            Scale = scale;
        }
    }
}