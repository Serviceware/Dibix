using System;
using System.Collections.Generic;
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
            const string commandText = @"SELECT [url] = N'https://localhost/FirstUrl', [url] = NULL
SELECT [url] = N'https://localhost/AnotherUrl'";
            using (IMultipleResultReader reader = accessor.QueryMultiple(commandText, CommandType.Text, ParametersVisitor.Empty))
            {
                Entity firstResult = reader.ReadSingle<Entity>([typeof(Entity), typeof(Uri)], "url");
                Assert.IsNotNull(firstResult.Url);
                Assert.AreEqual("https://localhost/FirstUrl", firstResult.Url.ToString());
                Assert.IsEmpty(firstResult.MoreUrls);
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
            public Uri? Url { get; set; }
            public IReadOnlyList<Uri> MoreUrls { get; } = new List<Uri>();
        }
    }
}