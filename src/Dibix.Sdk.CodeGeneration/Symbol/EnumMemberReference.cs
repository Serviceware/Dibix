namespace Dibix.Sdk.CodeGeneration
{
    public sealed class EnumMemberReference : ValueReference<SchemaTypeReference>
    {
        public EnumSchemaMember Member { get; }
        public EnumMemberReferenceKind Kind { get; }

        public EnumMemberReference(SchemaTypeReference type, EnumSchemaMember member, EnumMemberReferenceKind kind, SourceLocation location) : base(type, location)
        {
            Member = member;
            Kind = kind;
        }
    }
}