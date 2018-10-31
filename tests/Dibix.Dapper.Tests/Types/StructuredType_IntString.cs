namespace Dibix.Dapper.Tests
{
    internal class StructuredType_IntString : StructuredType_IntStringDecimal
    {
        public void Add(int intValue, string stringValue) => base.Add(intValue, stringValue, default);
    }
}