using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Dapper.Tests
{
    [TestClass]
    public class DateTimeKindTest : DapperTestBase
    {
        [TestMethod]
        public Task KindIsSpecified_AccordingToPropertySetting() => base.ExecuteTest(async accessor =>
        {
            const string commandText = "SELECT [creationtime] = CAST(43861.1666666667 AS DATETIME), [registrationtime] = CAST(43861.1666666667 AS DATETIME), [modifiedat] = CAST(43861.1666666667 AS DATETIME)";
            Entity entity = await accessor.QuerySingleAsync<Entity>(commandText, CommandType.Text, ParametersVisitor.Empty, cancellationToken: default).ConfigureAwait(false);
            Assert.AreEqual(DateTimeKind.Unspecified, entity.CreationTime.Kind);
            Assert.AreEqual(DateTimeKind.Utc, entity.RegistrationTime.Kind);
            Assert.IsNotNull(entity.ModifiedAt);
            Assert.AreEqual(DateTimeKind.Utc, entity.ModifiedAt.Value.Kind);
            Assert.IsNull(entity.DeletedAt);
        });

        private sealed class Entity
        {
            public int Id { get; set; }
            public DateTime CreationTime { get; set; }

            [DateTimeKind(DateTimeKind.Utc)]
            public DateTime RegistrationTime { get; set; }

            [DateTimeKind(DateTimeKind.Utc)]
            public DateTime? ModifiedAt { get; set; }
            
            [DateTimeKind(DateTimeKind.Utc)]
            public DateTime? DeletedAt { get; set; }
        }
    }
}