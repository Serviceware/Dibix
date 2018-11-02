namespace Dibix.Dapper.Tests
{
    internal class StructuredType_IntStringDecimal_Custom : StructuredType<StructuredType_IntStringDecimal_Custom, int, string, decimal>
    {
        public StructuredType_IntStringDecimal_Custom() : base(typeName: "eden_data_tests_structuredtype") => base.ImportSqlMetadata(() => this.Add(default, default, default));

        public void Add(int intValue, [SqlMetadata(MaxLength = 1)] string stringValue, [SqlMetadata(Scale = 0)] decimal decimalValue) => base.AddValues(intValue, stringValue, decimalValue);
    }
}