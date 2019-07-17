namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ObjectContractProperty
    {
        public string Name { get; }
        public string Type { get; }
        public bool IsPartOfKey { get; }
        public bool SkipNull { get; }
        public bool IsEnumerable { get; }

        public ObjectContractProperty(string name, string type, bool isPartOfKey, bool skipNull, bool isEnumerable)
        {
            this.Name = name;
            this.Type = type;
            this.IsPartOfKey = isPartOfKey;
            this.SkipNull = skipNull;
            this.IsEnumerable = isEnumerable;
        }
    }
}