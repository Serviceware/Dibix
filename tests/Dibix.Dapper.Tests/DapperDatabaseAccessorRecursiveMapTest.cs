using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Dapper.Tests
{
    [TestClass]
    public class DapperDatabaseAccessorRecursiveMapTest : DapperTestBase
    {
        [TestMethod]
        public Task QuerySingle() => ExecuteTest(accessor =>
        {
            const string commandText = @"SELECT [x].[id], [x].[name], [x].[parentid]
FROM (VALUES (10, N'Software',  NULL)
           , (11, N'Apple',     10)
           , (12, N'Microsoft', 10)) AS [x]([id], [name], [parentid])";
            Category software = accessor.QuerySingle<Category>(commandText, CommandType.Text, ParametersVisitor.Empty);
            Assert.IsNotNull(software);
            Assert.AreEqual(10, software.Id);
            Assert.AreEqual("Software", software.Name);
            Assert.IsNull(software.ParentId);
            Assert.HasCount(2, software.Categories);
            Assert.AreEqual(11, software.Categories[0].Id);
            Assert.AreEqual("Apple", software.Categories[0].Name);
            Assert.AreEqual(10, software.Categories[0].ParentId);
            Assert.AreEqual(12, software.Categories[1].Id);
            Assert.AreEqual("Microsoft", software.Categories[1].Name);
            Assert.AreEqual(10, software.Categories[1].ParentId);
        });

        [TestMethod]
        public Task GridResult_ReadMany() => ExecuteTest(accessor =>
        {
            const string commandText = @"SELECT [x].[id], [x].[name], [x].[parentid]
FROM (VALUES (10, N'Software',  NULL)
           , (11, N'Apple',     10)
           , (12, N'Microsoft', 10)) AS [x]([id], [name], [parentid])";
            using IMultipleResultReader reader = accessor.QueryMultiple(commandText, CommandType.Text, ParametersVisitor.Empty);
            IList<Category> categories = reader.ReadMany<Category>().ToArray();
            Assert.HasCount(1, categories);
            Assert.IsNotNull(categories[0]);
            Assert.AreEqual(10, categories[0].Id);
            Assert.AreEqual("Software", categories[0].Name);
            Assert.IsNull(categories[0].ParentId);
            Assert.HasCount(2, categories[0].Categories);
            Assert.AreEqual(11, categories[0].Categories[0].Id);
            Assert.AreEqual("Apple", categories[0].Categories[0].Name);
            Assert.AreEqual(10, categories[0].Categories[0].ParentId);
            Assert.AreEqual(12, categories[0].Categories[1].Id);
            Assert.AreEqual("Microsoft", categories[0].Categories[1].Name);
            Assert.AreEqual(10, categories[0].Categories[1].ParentId);
        });

        private sealed class Category
        {
            [Key]
            public int Id { get; set; }
            public string? Name { get; set; }
            public IList<Category> Categories { get; } = new Collection<Category>();

            [Discriminator]
            public int? ParentId { get; set; }
        }
    }
}