using System.Data;
using System.Threading.Tasks;
using Xunit;

namespace Dibix.Dapper.Tests
{
    public class TextObfuscationTest
    {
        [Fact]
        public async Task ObfuscateAndDeobfuscate_OriginalValueIsRestored()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = "SELECT [password] = @password";
                IParametersVisitor parameters = accessor.Parameters().SetString("password", "test", true).Build();
                Entity entity = await accessor.QuerySingleAsync<Entity>(commandText, CommandType.Text, parameters).ConfigureAwait(false);
                Assert.Equal("test", entity.Password);
            }
        }
    }
}