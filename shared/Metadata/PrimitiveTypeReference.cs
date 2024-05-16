namespace Dibix
{
    public sealed class PrimitiveTypeReference : TypeReference
    {
        public PrimitiveType Type { get; }
        public int? Size { get; }
        public override string DisplayName => $"{Type}";

        public PrimitiveTypeReference(PrimitiveType type, bool isNullable, bool isEnumerable, int? size = null, SourceLocation location = default) : base(isNullable, isEnumerable, location)
        {
            Type = type;
            Size = size;
        }
    }
}