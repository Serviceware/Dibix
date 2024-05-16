namespace Dibix.Sdk.CodeGeneration
{
    public sealed class NullValueReference : ValueReference
    {
        public NullValueReference(TypeReference type, SourceLocation location) : base(type, location)
        {
        }
    }
}