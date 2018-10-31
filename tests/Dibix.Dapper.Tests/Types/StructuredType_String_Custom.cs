namespace Dibix.Dapper.Tests
{
    internal class StructuredType_String_Custom : StructuredType_IntStringDecimal_Custom
    {
        public void Add(string stringValue) => base.Add(default, stringValue, default);
    }
}