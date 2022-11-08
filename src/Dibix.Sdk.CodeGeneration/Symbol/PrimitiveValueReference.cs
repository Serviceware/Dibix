namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PrimitiveValueReference : ValueReference<PrimitiveTypeReference>
    {
        public object Value { get; }

        public PrimitiveValueReference(PrimitiveTypeReference type, object value, string source, int line, int column) : base(type, source, line, column)
        {
            this.Value = value;
        }
    }
}