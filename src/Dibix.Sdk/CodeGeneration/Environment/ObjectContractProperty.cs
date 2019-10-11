namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ObjectContractProperty
    {
        public string Name { get; }
        public string Type { get; }
        public bool IsPartOfKey { get; }
        public bool IsDiscriminator { get; }
        public bool Obfuscated { get; }
        public SerializationBehavior SerializationBehavior { get; }
        public bool IsEnumerable { get; }

        public ObjectContractProperty(string name, string type, bool isPartOfKey, bool isDiscriminator, SerializationBehavior serializationBehavior, bool obfuscated, bool isEnumerable)
        {
            this.Name = name;
            this.Type = type;
            this.IsPartOfKey = isPartOfKey;
            this.IsDiscriminator = isDiscriminator;
            this.Obfuscated = obfuscated;
            this.SerializationBehavior = !isDiscriminator ? serializationBehavior : SerializationBehavior.Never;
            this.IsEnumerable = isEnumerable;
        }
    }
}