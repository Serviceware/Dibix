using System;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ObjectSchemaProperty : IPropertyDescriptor
    {
        private readonly Lazy<TypeReference> _typeResolver;
        private readonly Lazy<ValueReference> _defaultValueResolver;

        public Token<string> Name { get; }
        public TypeReference Type => this._typeResolver.Value;
        public ValueReference DefaultValue => this._defaultValueResolver.Value;
        public DateTimeKind DateTimeKind { get; }
        public SerializationBehavior SerializationBehavior { get; }
        public bool IsPartOfKey { get; }
        public bool IsOptional { get; }
        public bool IsDiscriminator { get; }
        public bool IsObfuscated { get; }
        public bool IsRelativeHttpsUrl { get; }
        string IPropertyDescriptor.Name => Name;

        public ObjectSchemaProperty(Token<string> name, TypeReference type, ValueReference defaultValue = default, SerializationBehavior serializationBehavior = default, DateTimeKind dateTimeKind = default, bool isPartOfKey = default, bool isOptional = default, bool isDiscriminator = default, bool isObfuscated = default, bool isRelativeHttpsUrl = default) : this(name, () => type, _ => defaultValue, serializationBehavior, dateTimeKind, isPartOfKey, isOptional, isDiscriminator, isObfuscated, isRelativeHttpsUrl) { }
        internal ObjectSchemaProperty(Token<string> name, Func<TypeReference> typeResolver, Func<TypeReference, ValueReference> defaultValueResolver, SerializationBehavior serializationBehavior, DateTimeKind dateTimeKind, bool isPartOfKey, bool isOptional, bool isDiscriminator, bool isObfuscated, bool isRelativeHttpsUrl)
        {
            this._typeResolver = new Lazy<TypeReference>(typeResolver);
            this._defaultValueResolver = new Lazy<ValueReference>(() => defaultValueResolver(this.Type));
            this.Name = name;
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