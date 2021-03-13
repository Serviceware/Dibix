namespace Dibix.Dapper.Tests
{
    internal class StructuredType_IntStringDecimal : StructuredType<StructuredType_IntStringDecimal, int, string, decimal>
    {
        public StructuredType_IntStringDecimal() : base(typeName: "_dibix_tests_structuredtype") => base.ImportSqlMetadata(() => this.Add(default, default, default));

        public void Add(int intValue, string stringValue, decimal decimalValue) => base.AddValues(intValue, stringValue, decimalValue);
    }
}