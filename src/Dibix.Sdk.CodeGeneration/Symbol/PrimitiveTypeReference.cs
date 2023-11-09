using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PrimitiveTypeReference : TypeReference
    {
        public PrimitiveType Type { get; }
        public int? Size { get; }
        public override string DisplayName => $"{Type}";

        public PrimitiveTypeReference(PrimitiveType type, bool isNullable, bool isEnumerable, int? size, SourceLocation location) : base(isNullable, isEnumerable, location)
        {
            Type = type;
            Size = size;
        }
    }
}