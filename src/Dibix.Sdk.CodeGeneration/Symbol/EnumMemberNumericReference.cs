using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumMemberNumericReference : ValueReference<SchemaTypeReference>
    {
        public int Value { get; }

        public EnumMemberNumericReference(SchemaTypeReference type, int value, SourceLocation location) : base(type, location)
        {
            this.Value = value;
        }
    }
}