using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace Dibix.Dapper.Tests
{
    public class TextObfuscationTest
    {
        [Fact]
        public void ObfuscateAndDeobfuscate_OriginalValueIsRestored()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = "SELECT [id] = 1, [password] = @password1, [id] = 1, [password] = @password2";
                InputClass input = new InputClass { password2 = "test2" };
                ParametersVisitor parameters = accessor.Parameters()
                                                       .SetString("password1", "test1", true)
                                                       .SetFromTemplate(input)
                                                       .Build();
                Entity entity = accessor.QuerySingle<Entity, Entity>(commandText, parameters, "id");
                Assert.Equal("test1", entity.Password);
                Assert.Equal("test2", entity.RelatedEntities.Single().Password);
            }
        }

        private sealed class InputClass
        {
            [Obfuscated]
            public string password2 { get; set; }
        }

        private sealed class Entity
        {
            public int Id { get; set; }
            
            [Obfuscated]
            public string Password { get; set; }
            public ICollection<Entity> RelatedEntities { get; }

            public Entity()
            {
                this.RelatedEntities = new Collection<Entity>();
            }
        }
    }
}