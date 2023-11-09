using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Dapper.Tests
{
    [TestClass]
    public class ParameterLengthTest : DapperTestBase
    {
        [TestMethod]
        public Task ParameterLengthSpecified_ValueTooLarge_ThrowsException() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT @param";
            ParametersVisitor parameters = accessor.Parameters()
                                                   .SetString("param", "value", size: 4)
                                                   .Build();
            DatabaseAccessException exception = AssertThrows<DatabaseAccessException>(() => accessor.QuerySingle<string>(commandText, CommandType.Text, parameters));
            Assert.AreEqual(DatabaseAccessErrorCode.ParameterSizeExceeded, exception.AdditionalErrorCode);
            Assert.AreEqual(@"Length of parameter 'param' is '5', which exceeds the supported size '4'
CommandType: Text
CommandText: <Inline>", exception.Message);
        });

        [TestMethod]
        public Task ParameterLengthSpecified_ValueEqual_NoExceptionThrown() => ExecuteTest(accessor =>
        {
            const string commandText = "SELECT @param";
            ParametersVisitor parameters = accessor.Parameters()
                                                   .SetString("param", "value", size: 5)
                                                   .Build();
            Assert.AreEqual("value", accessor.QuerySingle<string>(commandText, CommandType.Text, parameters));
        });
    }
}