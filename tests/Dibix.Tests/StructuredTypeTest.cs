using Xunit;

namespace Dibix.Tests
{
    public class StructuredTypeTest
    {
        [Fact]
        public void StructuredType_IntString_Dump()
        {
            X set = new X
            {
                { 2, "X" }
              , { 7, "y" }
            };
            string dump = set.Dump();

            Assert.Equal(@"intValue INT(4)  stringValue NVARCHAR(MAX)
---------------  -------------------------
2                X                        
7                y                        ", dump);
        }

        private class X : StructuredType<X, int, string>
        {
            public X() : base(null) => base.ImportSqlMetadata(() => this.Add(default, default));

            public void Add(int intValue, string stringValue) => base.AddValues(intValue, stringValue);
        }
    }
}
