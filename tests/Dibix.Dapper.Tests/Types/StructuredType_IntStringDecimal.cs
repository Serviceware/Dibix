using System.Data;

namespace Dibix.Dapper.Tests
{
    internal class StructuredType_IntStringDecimal : StructuredType<StructuredType_IntStringDecimal>
    {
        public override string TypeName => "_dibix_tests_structuredtype";

        public void Add(int intValue, string stringValue, decimal decimalValue) => AddRecord(intValue, stringValue, decimalValue);

        protected override void CollectMetadata(ISqlMetadataCollector collector)
        {
            collector.RegisterMetadata("intValue", SqlDbType.Int);
            collector.RegisterMetadata("stringValue", SqlDbType.NVarChar, maxLength: -1);
            collector.RegisterMetadata("decimalValue", SqlDbType.Decimal, precision: 14, scale: 2);
        }
    }
}