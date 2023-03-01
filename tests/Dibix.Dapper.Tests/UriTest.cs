using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Dapper.Tests
{
    [TestClass]
    public class UriTest : DapperTestBase
    {
        [TestMethod]
        public Task Read_ParseStringAsUri_UsingCustomDapperTypeHandler() => base.ExecuteTest(accessor =>
        {
            const string commandText = @"SELECT [url] = N'https://localhost/FirstUrl'
SELECT [url] = N'https://localhost/AnotherUrl'";
            using (IMultipleResultReader reader = accessor.QueryMultiple(commandText, CommandType.Text, ParametersVisitor.Empty))
            {
                Assert.AreEqual("https://localhost/FirstUrl", reader.ReadSingle<Entity>().Url.ToString());
                Assert.AreEqual("https://localhost/AnotherUrl", reader.ReadSingle<Uri>().ToString());
            }
        });

        [TestMethod]
        public Task Write_SetUriValueToString_UsingCustomDapperTypeHandler() => base.ExecuteTest(accessor =>
        {
            const string commandText = "SELECT @url";
            ParametersVisitor @params = accessor.Parameters()
                                                .SetFromTemplate(new
                                                {
                                                    url = new Uri("https://localhost/SomeUrl")
                                                })
                                                .Build();
            Assert.AreEqual("https://localhost/SomeUrl", accessor.QuerySingle<string>(commandText, CommandType.Text, @params));
        });

        private sealed class Entity
        {
            public Uri Url { get; set; }
        }
    }
}