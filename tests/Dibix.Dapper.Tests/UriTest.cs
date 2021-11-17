using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using Xunit;

namespace Dibix.Dapper.Tests
{
    public class UriTest : IClassFixture<DatabaseTestFixture>
    {
        private readonly DatabaseTestFixture _fixture;

        public UriTest(DatabaseTestFixture fixture) => this._fixture = fixture;

        [Fact]
        public void Read_ParseStringAsUri_UsingCustomDapperTypeHandler()
        {
            using (IDatabaseAccessor accessor = this._fixture.CreateDatabaseAccessor())
            {
                const string commandText = @"SELECT [url] = N'https://localhost/FirstUrl'
SELECT [url] = N'https://localhost/AnotherUrl'";
                using (IMultipleResultReader reader = accessor.QueryMultiple(commandText))
                {
                    Assert.Equal("https://localhost/FirstUrl", reader.ReadSingle<Entity>().Url.ToString());
                    Assert.Equal("https://localhost/AnotherUrl", reader.ReadSingle<Uri>().ToString());
                }
            }
        }

        [Fact]
        public void Write_SetUriValueToString_UsingCustomDapperTypeHandler()
        {
            using (IDatabaseAccessor accessor = this._fixture.CreateDatabaseAccessor())
            {
                const string commandText = "SELECT @url";
                ParametersVisitor @params = accessor.Parameters()
                                                    .SetFromTemplate(new
                                                    {
                                                        url = new Uri("https://localhost/SomeUrl")
                                                    })
                                                    .Build();
                Assert.Equal("https://localhost/SomeUrl", accessor.QuerySingle<string>(commandText, @params));
            }
        }

        private sealed class Entity
        {
            public Uri Url { get; set; }
        }
    }
}