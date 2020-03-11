namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PrimitiveTypeReference : TypeReference
    {
        public PrimitiveDataType Type { get; }

        public PrimitiveTypeReference(PrimitiveDataType type, bool isNullable, bool isEnumerable) : base(isNullable, isEnumerable)
        {
            this.Type = type;
        }
    }
}