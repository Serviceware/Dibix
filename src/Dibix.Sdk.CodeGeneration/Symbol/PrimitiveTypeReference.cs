using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PrimitiveTypeReference : TypeReference
    {
        public PrimitiveType Type { get; }
        public override string DisplayName => $"{this.Type}";

        public PrimitiveTypeReference(PrimitiveType type, bool isNullable, bool isEnumerable, SourceLocation location) : base(isNullable, isEnumerable, location)
        {
            this.Type = type;
        }
    }
}