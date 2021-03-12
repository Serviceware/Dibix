namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PrimitiveTypeReference : TypeReference
    {
        public PrimitiveType Type { get; }

        public PrimitiveTypeReference(PrimitiveType type, bool isNullable, bool isEnumerable) : base(isNullable, isEnumerable)
        {
            this.Type = type;
        }
    }
}