using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumMemberStringReference : ValueReference<SchemaTypeReference>
    {
        public string Value { get; }

        public EnumMemberStringReference(SchemaTypeReference type, string value, SourceLocation location) : base(type, location)
        {
            this.Value = value;
        }
    }
}