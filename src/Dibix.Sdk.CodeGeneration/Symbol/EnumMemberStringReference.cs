namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumMemberStringReference : ValueReference<SchemaTypeReference>
    {
        public string Value { get; }

        public EnumMemberStringReference(SchemaTypeReference type, string value, string source, int line, int column) : base(type, source, line, column)
        {
            this.Value = value;
        }
    }
}