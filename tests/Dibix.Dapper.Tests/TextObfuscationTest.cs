using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Dapper.Tests
{
    [TestClass]
    public class TextObfuscationTest : DapperTestBase
    {
        [TestMethod]
        public Task ObfuscateAndDeobfuscate_OriginalValueIsRestored() => base.ExecuteTest(accessor =>
        {
            const string commandText = "SELECT [id] = 1, [password] = @password1, [id] = 1, [password] = @password2";
            InputClass input = new InputClass { password2 = "test2" };
            ParametersVisitor parameters = accessor.Parameters()
                                                   .SetString("password1", "test1", true)
                                                   .SetFromTemplate(input)
                                                   .Build();
            Entity entity = accessor.QuerySingle<Entity>(commandText, CommandType.Text, parameters, new[] { typeof(Entity), typeof(Entity) }, "id");
            Assert.AreEqual("test1", entity.Password);
            Assert.AreEqual("test2", entity.RelatedEntities.Single().Password);
        });

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