using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SchemaTypeReference : TypeReference
    {
        public string Key { get; }
        public override string DisplayName => this.Key;

        public SchemaTypeReference(string key, bool isNullable, bool isEnumerable, SourceLocation location) : base(isNullable, isEnumerable, location)
        {
            this.Key = key;
        }
    }
}