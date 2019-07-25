namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ObjectContractProperty
    {
        public string Name { get; }
        public string Type { get; }
        public bool IsPartOfKey { get; }
        public SerializationBehavior SerializationBehavior { get; }
        public bool IsEnumerable { get; }

        public ObjectContractProperty(string name, string type, bool isPartOfKey, SerializationBehavior serializationBehavior, bool isEnumerable)
        {
            this.Name = name;
            this.Type = type;
            this.IsPartOfKey = isPartOfKey;
            this.SerializationBehavior = serializationBehavior;
            this.IsEnumerable = isEnumerable;
        }
    }
}