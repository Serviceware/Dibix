namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SchemaTypeReference : TypeReference
    {
        public string Key { get; }

        public SchemaTypeReference(string key, bool isNullable, bool isEnumerable, string source, int line, int column) : base(isNullable, isEnumerable, source, line, column)
        {
            this.Key = key;
        }

        public static SchemaTypeReference WithNamespace(string @namespace, string definitionName, string source, int line, int column, bool isNullable, bool isEnumerable)
        {
            return new SchemaTypeReference($"{@namespace}.{definitionName}", isNullable, isEnumerable, source, line, column);
        }
    }
}