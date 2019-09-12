using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Dibix.Tests
{
    public class MultiMapperTest
    {
        [Fact]
        public void AutoMap_WithoutProjection()
        {
            object[][] rows =
            {
                new object[] { new Character { Id = 1 }, new Name { FirstName = "Luke", LastName = "Skywalker" }, "Jedi Order", new Name { FirstName = "Obi-Wan", LastName = "Kenobi" } }
              , new object[] { new Character { Id = 1 }, new Name { FirstName = "Luke", LastName = "Skywalker" }, "Jedi Order", new Name { FirstName = "Yoda" } }
              , new object[] { new Character { Id = 1 }, new Name { FirstName = "Luke", LastName = "Skywalker" }, "New Republic", new Name { FirstName = "Obi-Wan", LastName = "Kenobi" } }
              , new object[] { new Character { Id = 1 }, new Name { FirstName = "Luke", LastName = "Skywalker" }, "New Republic", new Name { FirstName = "Yoda" } }
            };

            Character result = InvokeMultiMapFirst<Character>(false, rows);
            Assert.NotNull(result.Name);
            Assert.Equal("Luke", result.Name.FirstName);
            Assert.Equal("Skywalker", result.Name.LastName);
            Assert.Equal(2, result.Affiliations.Count);
            Assert.Equal("Jedi Order", result.Affiliations[0]);
            Assert.Equal("New Republic", result.Affiliations[1]);
            Assert.Equal(2, result.Masters.Count);
            Assert.Equal("Obi-Wan", result.Masters[0].FirstName);
            Assert.Equal("Kenobi", result.Masters[0].LastName);
            Assert.Equal("Yoda", result.Masters[1].FirstName);
            Assert.Null(result.Masters[1].LastName);
        }

        [Fact]
        public void AutoMap_WithRecursiveDiscriminator()
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

            IList<Category> categories = InvokeMultiMap<Category>(false, rows).ToArray();
            Assert.NotNull(categories);
            Assert.Equal(3, categories.Count);

            Assert.Equal(1, categories[0].Id);
            Assert.Equal("Hardware", categories[0].Name);
            Assert.Null(categories[0].ParentCategoryId);
            Assert.Equal(2, categories[0].Categories.Count);
            Assert.Equal(4, categories[0].Categories[0].Id);
            Assert.Equal("Computer", categories[0].Categories[0].Name);
            Assert.Equal(1, categories[0].Categories[0].ParentCategoryId);
            Assert.False(categories[0].Categories[0].Categories.Any());
            Assert.False(categories[0].Categories[0].Blacklist.Any());
            Assert.Equal(5, categories[0].Categories[1].Id);
            Assert.Equal("Monitor", categories[0].Categories[1].Name);
            Assert.Equal(1, categories[0].Categories[1].ParentCategoryId);
            Assert.False(categories[0].Categories[1].Categories.Any());
            Assert.False(categories[0].Categories[1].Blacklist.Any());
            Assert.False(categories[0].Blacklist.Any());
            Assert.Equal(2, categories[1].Id);
            Assert.Equal("Software", categories[1].Name);
            Assert.Null(categories[1].ParentCategoryId);
            Assert.Equal(2, categories[1].Categories.Count);
            Assert.Equal(6, categories[1].Categories[0].Id);
            Assert.Equal("Developer Tools", categories[1].Categories[0].Name);
            Assert.Equal(2, categories[1].Categories[0].ParentCategoryId);
            Assert.Equal(2, categories[1].Categories[0].Categories.Count);
            Assert.Equal(9, categories[1].Categories[0].Categories[0].Id);
            Assert.Equal("SQL Server", categories[1].Categories[0].Categories[0].Name);
            Assert.Equal(6, categories[1].Categories[0].Categories[0].ParentCategoryId);
            Assert.False(categories[1].Categories[0].Categories[0].Categories.Any());
            Assert.False(categories[1].Categories[0].Categories[0].Blacklist.Any());
            Assert.Equal(8, categories[1].Categories[0].Categories[1].Id);
            Assert.Equal("Visual Studio", categories[1].Categories[0].Categories[1].Name);
            Assert.Equal(6, categories[1].Categories[0].Categories[1].ParentCategoryId);
            Assert.False(categories[1].Categories[0].Categories[1].Categories.Any());
            Assert.False(categories[1].Categories[0].Categories[1].Blacklist.Any());
            Assert.False(categories[1].Categories[0].Blacklist.Any());
            Assert.Equal(7, categories[1].Categories[1].Id);
            Assert.Equal("Windows", categories[1].Categories[1].Name);
            Assert.Equal(2, categories[1].Categories[1].ParentCategoryId);
            Assert.False(categories[1].Categories[1].Categories.Any());
            Assert.False(categories[1].Categories[1].Blacklist.Any());
            Assert.Equal(2, categories[1].Blacklist.Count);
            Assert.Equal(1, categories[1].Blacklist[0].UserId);
            Assert.Equal(2, categories[1].Blacklist[1].UserId);
            Assert.Equal(3, categories[2].Id);
            Assert.Equal("Uncategorized", categories[2].Name);
            Assert.Null(categories[2].ParentCategoryId);
            Assert.False(categories[2].Categories.Any());
            Assert.False(categories[2].Blacklist.Any());
        }

        [Fact]
        public void AutoMap_WithProjection()
        {
            object[][] rows =
            {
                new object[] { new Name { FirstName = "Darth", LastName = "Vader" }, new Name { FirstName = "Anakin", LastName = "Skywalker" } }
            };

            CharacterInfo result = InvokeMultiMapFirst<CharacterInfo>(true, rows);
            Assert.NotNull(result.Name);
            Assert.Equal("Darth", result.Name.FirstName);
            Assert.Equal("Vader", result.Name.LastName);
            Assert.NotNull(result.Name);
            Assert.Equal("Anakin", result.AlternateName.FirstName);
            Assert.Equal("Skywalker", result.AlternateName.LastName);
        }

        private static TReturn InvokeMultiMapFirst<TReturn>(bool useProjection, object[][] rows) => InvokeMultiMap<TReturn>(useProjection, rows).ToArray().First();
        private static IEnumerable<TReturn> InvokeMultiMap<TReturn>(bool useProjection, IEnumerable<object[]> rows)
        {
            Type type = Type.GetType("Dibix.MultiMapper,Dibix", true);
            object instance = Activator.CreateInstance(type);
            MethodInfo mapRowMethod = type.GetMethod("MapRow").MakeGenericMethod(typeof(TReturn));
            MethodInfo postProcessMethod = type.GetMethod("PostProcess").MakeGenericMethod(typeof(TReturn));

            IEnumerable<TReturn> results = InvokeMultiMap<TReturn>(mapRowMethod, instance, useProjection, rows);
            results = (IEnumerable<TReturn>)postProcessMethod.Invoke(instance, new[] { results });
            return results;
        }
        private static IEnumerable<TReturn> InvokeMultiMap<TReturn>(MethodInfo method, object instance, bool useProjection, IEnumerable<object[]> rows)
        {
            return rows.Select(row => (TReturn)method.Invoke(instance, new object[] { useProjection, row }));
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
