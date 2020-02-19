namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ObjectContractProperty
    {
        public string Name { get; }
        public ContractPropertyType Type { get; }
        public bool IsPartOfKey { get; }
        public bool IsDiscriminator { get; }
        public bool Obfuscated { get; }
        public SerializationBehavior SerializationBehavior { get; }

        public ObjectContractProperty(string name, ContractPropertyType type, bool isPartOfKey, bool isDiscriminator, SerializationBehavior serializationBehavior, bool obfuscated)
        {
            this.Name = name;
            this.Type = type;
            this.IsPartOfKey = isPartOfKey;
            this.IsDiscriminator = isDiscriminator;
            this.Obfuscated = obfuscated;
            this.SerializationBehavior = !isDiscriminator ? serializationBehavior : SerializationBehavior.Never;
        }
    }
}