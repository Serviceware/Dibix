using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Tests
{
    [TestClass]
    public class StructuredTypeTest
    {
        [TestMethod]
        public void StructuredType_IntString_Dump()
        {
            X set = new X
            {
                { 2, "X" }
              , { 7, "y" }
            };
            string dump = set.Dump();

            Assert.AreEqual(@"intValue INT(4)  stringValue NVARCHAR(MAX)
---------------  -------------------------
2                X                        
7                y                        ", dump);
        }

        private class X : StructuredType<X, int, string>
        {
            public X() : base("x") => base.ImportSqlMetadata(() => this.Add(default, default));

            public void Add(int intValue, string stringValue) => base.AddValues(intValue, stringValue);
        }
    }
}
