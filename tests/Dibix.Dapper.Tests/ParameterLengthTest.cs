using System;
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
            IParameterBuilder parameterBuilder = accessor.Parameters();
            InvalidOperationException exception = AssertThrows<InvalidOperationException>(() => parameterBuilder.SetString("param", "value", size: 4));
            Assert.AreEqual("""
                            The value for parameter 'param' has a length of 5 which exceeds the maximum length of the data type (4)
                            -
                            Value: value
                            """, exception.Message);
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