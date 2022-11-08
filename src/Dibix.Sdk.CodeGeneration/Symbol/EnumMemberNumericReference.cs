namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumMemberNumericReference : ValueReference<SchemaTypeReference>
    {
        public int Value { get; }

        public EnumMemberNumericReference(SchemaTypeReference type, int value, string source, int line, int column) : base(type, source, line, column)
        {
            this.Value = value;
        }
    }
}