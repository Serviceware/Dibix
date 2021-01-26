using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace Dibix.Dapper.Tests
{
    public class DapperDatabaseAccessorAutoMultiMapTest
    {
        [Fact]
        public void QuerySingle_WithAutoMultiMap_RecursiveTreeModel()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT [x].[identifier], [y].[identifier]
FROM (VALUES (N'desks')) AS [x]([identifier])
INNER JOIN (VALUES (N'agentdesk', N'desks')
                 , (N'workingdesk', N'desks')) AS [y]([identifier], [identifier_parent]) ON [x].[identifier] = [y].[identifier_parent]";
                RecursiveEntity result = accessor.QuerySingle<RecursiveEntity, RecursiveEntity>(commandText, accessor.Parameters().Build(), "identifier");
                Assert.Equal("desks", result.Identifier);
                Assert.Equal(2, result.Children.Count);
                Assert.Equal("agentdesk", result.Children[0].Identifier);
                Assert.Equal("workingdesk", result.Children[1].Identifier);
            }
        }

        [Fact]
        public void QuerySingle_WithAutoMultiMap_MultipleNestedPaths()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT [x].[name], [y].[name], [z].[name]
FROM (VALUES (N'feature1')) AS [x]([name])
INNER JOIN (VALUES (N'black', N'feature1')
                 , (N'red', N'feature1')) AS [y]([name], [name_feature]) ON [x].[name] = [y].[name_feature]
INNER JOIN (VALUES (N'dependentfeaturex', N'feature1')
                 , (N'dependentfeaturey', N'feature1')) AS [z]([name], [name_feature]) ON [x].[name] = [z].[name_feature]";
                FeatureEntity result = accessor.QuerySingle<FeatureEntity, FeatureItemEntity, DependentFeatureEntity>(commandText, accessor.Parameters().Build(), "name,name");
                Assert.Equal("feature1", result.Name);
                Assert.Equal(2, result.Items.Count);
                Assert.Equal("black", result.Items[0].Name);
                Assert.Equal("red", result.Items[1].Name);
                Assert.Equal(2, result.Dependencies.Count);
                Assert.Equal("dependentfeaturex", result.Dependencies[0].Name);
                Assert.Equal("dependentfeaturey", result.Dependencies[1].Name);
            }
        }

        [Fact]
        public void QuerySingle_WithAutoMultiMap_SingleDuplicateProperty_SetOnlyOnce()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT [x].[name], [y].[name], [z].[name]
FROM (VALUES (N'feature1')) AS [x]([name])
INNER JOIN (VALUES (N'feature1_base', N'feature1')) AS [y]([name], [name_feature]) ON [x].[name] = [y].[name_feature]
INNER JOIN (VALUES (N'black', N'feature1')
                 , (N'red', N'feature1')) AS [z]([name], [name_feature]) ON [x].[name] = [z].[name_feature]";
                FeatureEntity result = accessor.QuerySingle<FeatureEntity, FeatureBaseEntity, FeatureItemEntity>(commandText, accessor.Parameters().Build(), "name,name");
                Assert.Equal("feature1", result.Name);
                Assert.Equal(2, result.Items.Count);
                Assert.Equal("black", result.Items[0].Name);
                Assert.Equal("red", result.Items[1].Name);
                Assert.NotNull(result.Base);
                Assert.Equal("feature1_base", result.Base.Name);
            }
        }

        [Fact]
        public void QuerySingle_WithAutoMultiMap_AmbiguousKey_IdentifiedUsingParent()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT [x].[name], [xt].[lcid], [xt].[value], [y].[name], [yt].[lcid], [yt].[value]
FROM (VALUES (N'feature1')) AS [x]([name])
INNER JOIN (VALUES (7, N'feature_de', N'feature1')
                 , (9, N'feature_en', N'feature1')) AS [xt]([lcid], [value], [name_feature]) ON [x].[name] = [xt].[name_feature]
INNER JOIN (VALUES (N'black', N'feature1')
                 , (N'red', N'feature1')) AS [y]([name], [name_feature]) ON [x].[name] = [y].[name_feature]
INNER JOIN (VALUES (7, N'black_de', N'black')
                 , (9, N'black_en', N'black')
				 , (7, N'red_de', N'red')
                 , (9, N'red_en', N'red')) AS [yt]([lcid], [value], [name_item]) ON [y].[name] = [yt].[name_item]";
                FeatureEntity result = accessor.QuerySingle<FeatureEntity, TranslationEntity, FeatureItemEntity, TranslationEntity>(commandText, accessor.Parameters().Build(), "lcid,name,lcid");
                Assert.Equal("feature1", result.Name);
                Assert.Equal(2, result.Items.Count);
                Assert.Equal("black", result.Items[0].Name);
                Assert.Equal("red", result.Items[1].Name);
                Assert.Equal(2, result.Translations.Count);
                Assert.Equal(7, result.Translations[0].Lcid);
                Assert.Equal("feature_de", result.Translations[0].Value);
                Assert.Equal(9, result.Translations[1].Lcid);
                Assert.Equal("feature_en", result.Translations[1].Value);
                Assert.Equal(2, result.Items[0].Translations.Count);
                Assert.Equal(7, result.Items[0].Translations[0].Lcid);
                Assert.Equal("black_de", result.Items[0].Translations[0].Value);
                Assert.Equal(9, result.Items[0].Translations[1].Lcid);
                Assert.Equal("black_en", result.Items[0].Translations[1].Value);
                Assert.Equal(2, result.Items[1].Translations.Count);
                Assert.Equal(7, result.Items[1].Translations[0].Lcid);
                Assert.Equal("red_de", result.Items[1].Translations[0].Value);
                Assert.Equal(9, result.Items[1].Translations[1].Lcid);
                Assert.Equal("red_en", result.Items[1].Translations[1].Value);
            }
        }

        [Fact]
        public void QuerySingle_WithAutoMultiMap_NonEntity_Collection_ReferenceEqualityIsPerformed()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT [x].[name], [y].[name], [z].[data]
FROM (VALUES (N'feature1')) AS [x]([name])
INNER JOIN (VALUES (N'black', N'feature1')
                 , (N'red', N'feature1')) AS [y]([name], [name_feature]) ON [x].[name] = [y].[name_feature]
INNER JOIN (VALUES (0x1, N'feature1')
                 , (0x2, N'feature1')) AS [z]([data], [feature]) ON [x].[name] = [z].[feature]";
                FeatureEntity result = accessor.QuerySingle<FeatureEntity, FeatureItemEntity, byte[]>(commandText, accessor.Parameters().Build(), "name,data");
                Assert.Equal("feature1", result.Name);
                Assert.Equal(2, result.Items.Count);
                Assert.Equal("black", result.Items[0].Name);
                Assert.Equal("red", result.Items[1].Name);
                Assert.Equal(2, result.Pictures.Count);
                Assert.True(result.Pictures[0].SequenceEqual(Enumerable.Repeat((byte)1, 1)));
                Assert.True(result.Pictures[1].SequenceEqual(Enumerable.Repeat((byte)2, 1)));
            }
        }

        [Fact]
        public void QuerySingle_WithAutoMultiMap_NonEntity_SingleProperty_ReferenceEqualityIsPerformed()
        {
            using (IDatabaseAccessor accessor = DatabaseAccessorFactory.Create())
            {
                const string commandText = @"SELECT [x].[name], [y].[data], [z].[name]
FROM (VALUES (N'product1')) AS [x]([name])
INNER JOIN (VALUES (0x1, N'product1')
                 , (0x2, N'product1')) AS [y]([data], [product]) ON [x].[name] = [y].[product]
INNER JOIN (VALUES (N'feature1', N'product1')
                 , (N'feature2', N'product1')) AS [z]([name], [name_product]) ON [x].[name] = [z].[name_product]";
                ProductEntity result = accessor.QuerySingle<ProductEntity, byte[], FeatureEntity>(commandText, accessor.Parameters().Build(), "data,name");
                Assert.Equal("product1", result.Name);
                Assert.True(result.Picture.SequenceEqual(Enumerable.Repeat((byte)1, 1)));
                Assert.Equal(2, result.Features.Count);
                Assert.Equal("feature1", result.Features[0].Name);
                Assert.Equal("feature2", result.Features[1].Name);
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
            public IList<TranslationEntity> Translations { get; }
            public IList<FeatureItemEntity> Items { get; }
            public IList<DependentFeatureEntity> Dependencies { get; }
            public IList<byte[]> Pictures { get; }

            public FeatureEntity()
            {
                this.Translations = new Collection<TranslationEntity>();
                this.Items = new Collection<FeatureItemEntity>();
                this.Dependencies = new Collection<DependentFeatureEntity>();
                this.Pictures = new Collection<byte[]>();
            }
        }

        private class TranslationEntity
        {
            [Key]
            public int Lcid { get; set; }
            public string Value { get; set; }
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
            public IList<TranslationEntity> Translations { get; }

            public FeatureItemEntity()
            {
                this.Translations = new Collection<TranslationEntity>();
            }
        }

        private class DependentFeatureEntity
        {
            [Key]
            public string Name { get; set; }
        }

        private class ProductEntity
        {
            [Key]
            public string Name { get; set; }
            public byte[] Picture { get; set; }
            public IList<FeatureEntity> Features { get; }

            public ProductEntity()
            {
                this.Features = new Collection<FeatureEntity>();
            }
        }
    }
}
