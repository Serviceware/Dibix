using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Dibix.Tests
{
    [TestClass]
    public class MappingTest
    {
        [TestMethod]
        public void MultiMapper_WithoutProjection()
        {
            object[][] rows =
            {
                new object[] { new Character { Id = 1 }, new Name { FirstName = "Luke", LastName = "Skywalker" }, "Jedi Order", new Name { FirstName = "Obi-Wan", LastName = "Kenobi" } }
              , new object[] { new Character { Id = 1 }, new Name { FirstName = "Luke", LastName = "Skywalker" }, "Jedi Order", new Name { FirstName = "Yoda" } }
              , new object[] { new Character { Id = 1 }, new Name { FirstName = "Luke", LastName = "Skywalker" }, "New Republic", new Name { FirstName = "Obi-Wan", LastName = "Kenobi" } }
              , new object[] { new Character { Id = 1 }, new Name { FirstName = "Luke", LastName = "Skywalker" }, "New Republic", new Name { FirstName = "Yoda" } }
            };

            Mock<DbConnection> connection = new Mock<DbConnection>(MockBehavior.Strict);
            Mock<DatabaseAccessor> accessor = new Mock<DatabaseAccessor>(MockBehavior.Strict, connection.Object);
            Mock<ParametersVisitor> parametersVisitor = new Mock<ParametersVisitor>(MockBehavior.Strict);

            connection.Protected().Setup(nameof(IDisposable.Dispose), exactParameterMatch: true, false);
            accessor.Protected()
                    .Setup<IEnumerable<Character>>("QueryMany", new[] { typeof(Character) }, exactParameterMatch: true, "commandText", CommandType.Text, ItExpr.Is<ParametersVisitor>(x => x == parametersVisitor.Object), new[] { typeof(Character), typeof(Name), typeof(string), typeof(Name) }, ItExpr.IsAny<Func<object[], Character>>(), "x,x,x")
                    .Returns<string, CommandType, ParametersVisitor, Type[], Func<object[], Character>, string>((_, _, _, _, map, _) => rows.Select(map));

            Character result = accessor.Object.QuerySingle<Character>("commandText", CommandType.Text, parametersVisitor.Object, new[] { typeof(Character), typeof(Name), typeof(string), typeof(Name) }, "x,x,x");
            Assert.IsNotNull(result.Name);
            Assert.AreEqual("Luke", result.Name.FirstName);
            Assert.AreEqual("Skywalker", result.Name.LastName);
            Assert.AreEqual(2, result.Affiliations.Count);
            Assert.AreEqual("Jedi Order", result.Affiliations[0]);
            Assert.AreEqual("New Republic", result.Affiliations[1]);
            Assert.AreEqual(2, result.Masters.Count);
            Assert.AreEqual("Obi-Wan", result.Masters[0].FirstName);
            Assert.AreEqual("Kenobi", result.Masters[0].LastName);
            Assert.AreEqual("Yoda", result.Masters[1].FirstName);
            Assert.IsNull(result.Masters[1].LastName);
        }

        [TestMethod]
        public void MultiMapper_And_RecursiveMapper_WithoutProjection()
        {
            object[][] rows =
            {
                new object[] { new Category { Id = 9, Name = "SQL Server", ParentCategoryId = 6 },      null }
              , new object[] { new Category { Id = 1, Name = "Hardware" },                              null }
              , new object[] { new Category { Id = 2, Name = "Software" },                              new CategoryBlacklistEntry { UserId = 1 } }
              , new object[] { new Category { Id = 2, Name = "Software" },                              new CategoryBlacklistEntry { UserId = 2 } }
              , new object[] { new Category { Id = 3, Name = "Uncategorized" },                         null }
              , new object[] { new Category { Id = 4, Name = "Computer", ParentCategoryId = 1 },        null }
              , new object[] { new Category { Id = 5, Name = "Monitor", ParentCategoryId = 1 },         null }
              , new object[] { new Category { Id = 6, Name = "Developer Tools", ParentCategoryId = 2 }, null }
              , new object[] { new Category { Id = 7, Name = "Windows", ParentCategoryId = 2 },         null }
              , new object[] { new Category { Id = 8, Name = "Visual Studio", ParentCategoryId = 6 },   null }
            };

            Mock<DbConnection> connection = new Mock<DbConnection>(MockBehavior.Strict);
            Mock<DatabaseAccessor> accessor = new Mock<DatabaseAccessor>(connection.Object);
            Mock<ParametersVisitor> parametersVisitor = new Mock<ParametersVisitor>(MockBehavior.Strict);

            connection.Protected().Setup(nameof(IDisposable.Dispose), exactParameterMatch: true, false);
            accessor.Protected()
                    .Setup<IEnumerable<Category>>("QueryMany", new[] { typeof(Category) }, exactParameterMatch: true, "commandText", CommandType.Text, ItExpr.Is<ParametersVisitor>(x => x == parametersVisitor.Object), new[] { typeof(Category), typeof(CategoryBlacklistEntry) }, ItExpr.IsAny<Func<object[], Category>>(), "x")
                    .Returns<string, CommandType, ParametersVisitor, Type[], Func<object[], Category>, string>((_, _, _, _, map, _) => rows.Select(map));

            IList<Category> categories = accessor.Object.QueryMany<Category>("commandText", CommandType.Text, parametersVisitor.Object, new[] { typeof(Category), typeof(CategoryBlacklistEntry) }, "x").ToArray();
            Assert.IsNotNull(categories);
            Assert.AreEqual(3, categories.Count);

            Assert.AreEqual(1, categories[0].Id);
            Assert.AreEqual("Hardware", categories[0].Name);
            Assert.IsNull(categories[0].ParentCategoryId);
            Assert.AreEqual(2, categories[0].Categories.Count);
            Assert.AreEqual(4, categories[0].Categories[0].Id);
            Assert.AreEqual("Computer", categories[0].Categories[0].Name);
            Assert.AreEqual(1, categories[0].Categories[0].ParentCategoryId);
            Assert.IsFalse(categories[0].Categories[0].Categories.Any());
            Assert.IsFalse(categories[0].Categories[0].Blacklist.Any());
            Assert.AreEqual(5, categories[0].Categories[1].Id);
            Assert.AreEqual("Monitor", categories[0].Categories[1].Name);
            Assert.AreEqual(1, categories[0].Categories[1].ParentCategoryId);
            Assert.IsFalse(categories[0].Categories[1].Categories.Any());
            Assert.IsFalse(categories[0].Categories[1].Blacklist.Any());
            Assert.IsFalse(categories[0].Blacklist.Any());
            Assert.AreEqual(2, categories[1].Id);
            Assert.AreEqual("Software", categories[1].Name);
            Assert.IsNull(categories[1].ParentCategoryId);
            Assert.AreEqual(2, categories[1].Categories.Count);
            Assert.AreEqual(6, categories[1].Categories[0].Id);
            Assert.AreEqual("Developer Tools", categories[1].Categories[0].Name);
            Assert.AreEqual(2, categories[1].Categories[0].ParentCategoryId);
            Assert.AreEqual(2, categories[1].Categories[0].Categories.Count);
            Assert.AreEqual(9, categories[1].Categories[0].Categories[0].Id);
            Assert.AreEqual("SQL Server", categories[1].Categories[0].Categories[0].Name);
            Assert.AreEqual(6, categories[1].Categories[0].Categories[0].ParentCategoryId);
            Assert.IsFalse(categories[1].Categories[0].Categories[0].Categories.Any());
            Assert.IsFalse(categories[1].Categories[0].Categories[0].Blacklist.Any());
            Assert.AreEqual(8, categories[1].Categories[0].Categories[1].Id);
            Assert.AreEqual("Visual Studio", categories[1].Categories[0].Categories[1].Name);
            Assert.AreEqual(6, categories[1].Categories[0].Categories[1].ParentCategoryId);
            Assert.IsFalse(categories[1].Categories[0].Categories[1].Categories.Any());
            Assert.IsFalse(categories[1].Categories[0].Categories[1].Blacklist.Any());
            Assert.IsFalse(categories[1].Categories[0].Blacklist.Any());
            Assert.AreEqual(7, categories[1].Categories[1].Id);
            Assert.AreEqual("Windows", categories[1].Categories[1].Name);
            Assert.AreEqual(2, categories[1].Categories[1].ParentCategoryId);
            Assert.IsFalse(categories[1].Categories[1].Categories.Any());
            Assert.IsFalse(categories[1].Categories[1].Blacklist.Any());
            Assert.AreEqual(2, categories[1].Blacklist.Count);
            Assert.AreEqual(1, categories[1].Blacklist[0].UserId);
            Assert.AreEqual(2, categories[1].Blacklist[1].UserId);
            Assert.AreEqual(3, categories[2].Id);
            Assert.AreEqual("Uncategorized", categories[2].Name);
            Assert.IsNull(categories[2].ParentCategoryId);
            Assert.IsFalse(categories[2].Categories.Any());
            Assert.IsFalse(categories[2].Blacklist.Any());
        }

        [TestMethod]
        public void MultiMapper_WithProjection()
        {
            object[][] rows =
            {
                new object[] { new Name { FirstName = "Darth", LastName = "Vader" }, new Name { FirstName = "Anakin", LastName = "Skywalker" } }
            };

            Mock<DbConnection> connection = new Mock<DbConnection>(MockBehavior.Strict);
            Mock<DatabaseAccessor> accessor = new Mock<DatabaseAccessor>(MockBehavior.Strict, connection.Object);
            Mock<MultipleResultReader> multipleResultReader = new Mock<MultipleResultReader>(MockBehavior.Strict, null, null, null, false);

            connection.Protected().Setup(nameof(IDisposable.Dispose), exactParameterMatch: true, false);
            accessor.Protected()
                    .Setup<IMultipleResultReader>("QueryMultiple", exactParameterMatch: true, "commandText", CommandType.Text, ItExpr.Is<ParametersVisitor>(x => x == ParametersVisitor.Empty))
                    .Returns(multipleResultReader.Object);
            multipleResultReader.Setup(x => x.Dispose());
            multipleResultReader.Protected()
                                .Setup<IEnumerable<CharacterInfo>>("ReadMany", new[] { typeof(CharacterInfo) }, exactParameterMatch: true, new[] { typeof(Name), typeof(Name) }, ItExpr.IsAny<Func<object[], CharacterInfo>>(), "x")
                                .Returns<Type[], Func<object[], CharacterInfo>, string>((_, map, _) => rows.Select(map));


            using (IMultipleResultReader reader = ((IDatabaseAccessor)accessor.Object).QueryMultiple("commandText", CommandType.Text, ParametersVisitor.Empty))
            {
                CharacterInfo result = reader.ReadMany<CharacterInfo>(new[] { typeof(Name), typeof(Name) }, "x").Single();
                Assert.IsNotNull(result.Name);
                Assert.AreEqual("Darth", result.Name.FirstName);
                Assert.AreEqual("Vader", result.Name.LastName);
                Assert.IsNotNull(result.Name);
                Assert.AreEqual("Anakin", result.AlternateName.FirstName);
                Assert.AreEqual("Skywalker", result.AlternateName.LastName);
            }
        }

        private sealed class Character
        {
            [Key]
            public int Id { get; set; }

            public Name Name { get; set; }
            public IList<string> Affiliations { get; }
            public IList<Name> Masters { get; }

            public Character()
            {
                this.Affiliations = new Collection<string>();
                this.Masters = new Collection<Name>();
            }
        }

        private sealed class CharacterInfo
        {
            public Name Name { get; set; }
            public Name AlternateName { get; set; }
        }

        private sealed class Name
        {
            [Key]
            public string FirstName { get; set; }

            [Key]
            public string LastName { get; set; }
        }

        private sealed class Category
        {
            [Key]
            public int Id { get; set; }
            public string Name { get; set; }
            public IList<Category> Categories { get; }
            public IList<CategoryBlacklistEntry> Blacklist { get; }

            [Discriminator]
            public int? ParentCategoryId { get; set; }

            public Category()
            {
                this.Categories = new Collection<Category>();
                this.Blacklist = new Collection<CategoryBlacklistEntry>();
            }
        }

        private sealed class CategoryBlacklistEntry
        {
            [Key]
            public int UserId { get; set; }
        }
    }
}
