namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SchemaTypeReference : TypeReference
    {
        public string Key { get; }
        public string Source { get; }
        public int Line { get; }
        public int Column { get; }

        public SchemaTypeReference(string key, string source, int line, int column, bool isNullable, bool isEnumerable) : base(isNullable, isEnumerable)
        {
            this.Key = key;
            this.Source = source;
            this.Line = line;
            this.Column = column;
        }

        public static SchemaTypeReference WithNamespace(string @namespace, string definitionName, string source, int line, int column, bool isNullable, bool isEnumerable)
        {
            return new SchemaTypeReference($"{@namespace}.{definitionName}", source, line, column, isNullable, isEnumerable);
        }
    }
}