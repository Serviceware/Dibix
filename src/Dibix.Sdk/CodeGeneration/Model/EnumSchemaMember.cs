namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumSchemaMember
    {
        public string Name { get; }
        public string Value { get; }

        public EnumSchemaMember(string name) : this(name, null) { }
        public EnumSchemaMember(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }
    }
}