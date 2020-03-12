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
                const string commandText = "SELECT [id] = 1, [password] = @password, [id] = 1, [password] = @password";
                IParametersVisitor parameters = accessor.Parameters().SetString("password", "test", true).Build();
                Entity entity = accessor.QuerySingle<Entity, Entity>(commandText, parameters, "id");
                Assert.Equal("test", entity.Password);
                Assert.Equal("test", entity.RelatedEntities.Single().Password);
            }
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