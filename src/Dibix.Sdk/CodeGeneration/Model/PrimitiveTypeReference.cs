namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PrimitiveTypeReference : TypeReference
    {
        public PrimitiveType Type { get; }

        public PrimitiveTypeReference(PrimitiveType type, bool isNullable, bool isEnumerable, string source, int line, int column) : base(isNullable, isEnumerable, source, line, column)
        {
            this.Type = type;
        }
    }
}