using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PrimitiveValueReference : ValueReference<PrimitiveTypeReference>
    {
        public object Value { get; }

        public PrimitiveValueReference(PrimitiveTypeReference type, object value, SourceLocation location) : base(type, location)
        {
            this.Value = value;
        }
    }
}