using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Dibix.Dapper.Tests
{
    public class DapperDatabaseAccessorAutoMultiMapTest
    {
        [Fact]
        public void QuerySingle_WithAutoMultiMap_T2_Success()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessor.Create())
            {
                const string commandText = @"SELECT N'desks' AS [identifier], N'agentdesk' AS [identifier]
UNION ALL
SELECT N'desks' AS [identifier], N'workingdesk' AS [identifier]";
                RecursiveEntity result = accessor.QuerySingle<RecursiveEntity, RecursiveEntity>(commandText, accessor.Parameters().Build(), "identifier");
                Assert.Equal("desks", result.Identifier);
                Assert.Equal(2, result.Children.Count);
                Assert.Equal("agentdesk", result.Children[0].Identifier);
                Assert.Equal("workingdesk", result.Children[1].Identifier);
            }
        }

        [Fact]
        public void QuerySingle_WithAutoMultiMap_T3_Success()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessor.Create())
            {
                const string commandText = @"SELECT [name] = N'feature1', [name] = N'black', [name] = N'dependentfeaturex'
UNION ALL
SELECT [name] = N'feature1', [name] = N'black', [name] = N'dependentfeaturey'
UNION ALL
SELECT [name] = N'feature1', [name] = N'red', [name] = N'dependentfeaturex'
UNION ALL
SELECT [name] = N'feature1', [name] = N'red', [name] = N'dependentfeaturey'";
                FeatureEntity result = accessor.QuerySingle<FeatureEntity, FeatureItemEntity, DependentFeatureEntity>(commandText, accessor.Parameters().Build(), "name,name");
            }
        }

        private class RecursiveEntity
        {
            [Key]
            public string Identifier { get; set; }
            public IList<RecursiveEntity> Children { get; set; }

            public RecursiveEntity()
            {
                this.Children = new Collection<RecursiveEntity>();
            }
        }

        private class FeatureEntity
        {
            [Key]
            public string Name { get; set; }
            public FeatureBaseEntity Base { get; set; }
            public ICollection<FeatureItemEntity> Items { get; }
            public ICollection<DependentFeatureEntity> Dependencies { get; }

            public FeatureEntity()
            {
                this.Items = new Collection<FeatureItemEntity>();
                this.Dependencies = new Collection<DependentFeatureEntity>();
            }
        }

        private class FeatureBaseEntity
        {
            [Key]
            public string Name { get; set; }
        }

        private class FeatureItemEntity
        {
            [Key]
            public string Name { get; set; }
        }

        private class DependentFeatureEntity
        {
            [Key]
            public string Name { get; set; }
        }
    }
}
