namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SchemaTypeReference : TypeReference
    {
        public string Key { get; }
        public override string DisplayName => this.Key;

        public SchemaTypeReference(string key, bool isNullable, bool isEnumerable, string source, int line, int column) : base(isNullable, isEnumerable, source, line, column)
        {
            this.Key = key;
        }
    }
}