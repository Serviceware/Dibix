namespace Dibix.Sdk.CodeGeneration
{
    public sealed class NullValueReference : ValueReference
    {
        public NullValueReference(TypeReference type, string source, int line, int column) : base(type, source, line, column)
        {
        }
    }
}