using System;
using System.Threading.Tasks;
using Xunit;

namespace Dibix.Dapper.Tests
{
    public class DateTimeKindTest
    {
        [Fact]
        public async Task KindIsSpecified_AccordingToPropertySetting()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = "SELECT [creationtime] = CAST(43861.1666666667 AS DATETIME), [modifiedat] = CAST(43861.1666666667 AS DATETIME)";
                Entity entity = await accessor.QuerySingleAsync<Entity>(commandText, cancellationToken: default).ConfigureAwait(false);
                Assert.Equal(DateTimeKind.Unspecified, entity.CreationTime.Kind);
                Assert.NotNull(entity.ModifiedAt);
                Assert.Equal(DateTimeKind.Utc, entity.ModifiedAt.Value.Kind);
                Assert.Null(entity.DeletedAt);
            }
        }

        private sealed class Entity
        {
            public int Id { get; set; }
            public DateTime CreationTime { get; set; }
            
            [DateTimeKind(DateTimeKind.Utc)]
            public DateTime? ModifiedAt { get; set; }
            
            [DateTimeKind(DateTimeKind.Utc)]
            public DateTime? DeletedAt { get; set; }
        }
    }
}