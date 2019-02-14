using Xunit;

namespace Dibix.Dapper.Tests
{
    public class StructuredTypeTest
    {
        [Fact]
        public void StructuredType_IntString_Dump()
        {
            StructuredType_IntString set = new StructuredType_IntString
            {
                { 2, "X" }
              , { 7, "y" }
            };
            string dump = set.Dump();

            Assert.Equal(@"intValue INT(4)  stringValue NVARCHAR(MAX)  decimalValue DECIMAL(9)
---------------  -------------------------  -----------------------
2                X                          0                      
7                y                          0                      ", dump);
        }
    }
}
