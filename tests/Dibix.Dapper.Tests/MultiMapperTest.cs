using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Dibix.Dapper.Tests
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
        private static IEnumerable<TReturn> InvokeMultiMap<TReturn>(bool useProjection, object[][] rows)
        {
            Type type = Type.GetType("Dibix.MultiMapper,Dibix", true);
            object instance = Activator.CreateInstance(type);
            MethodInfo method = type.GetMethod("AutoMap");

            foreach (object[] row in rows)
            {
                yield return (TReturn)method.MakeGenericMethod(typeof(TReturn)).Invoke(instance, new object[] { useProjection, row });
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
    }
}
