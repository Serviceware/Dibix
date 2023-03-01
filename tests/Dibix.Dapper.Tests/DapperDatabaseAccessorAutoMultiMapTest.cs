using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Dapper.Tests
{
    [TestClass]
    public class DapperDatabaseAccessorAutoMultiMapTest : DapperTestBase
    {
        [TestMethod]
        public Task QuerySingle_WithAutoMultiMap_RecursiveTreeModel() => base.ExecuteTest(accessor =>
        {
            const string commandText = @"SELECT [x].[identifier], [y].[identifier]
FROM (VALUES (N'desks')) AS [x]([identifier])
INNER JOIN (VALUES (N'agentdesk', N'desks')
                 , (N'workingdesk', N'desks')) AS [y]([identifier], [identifier_parent]) ON [x].[identifier] = [y].[identifier_parent]";
            RecursiveEntity result = accessor.QuerySingle<RecursiveEntity>(commandText, CommandType.Text, accessor.Parameters().Build(), new[] { typeof(RecursiveEntity), typeof(RecursiveEntity) }, "identifier");
            Assert.AreEqual("desks", result.Identifier);
            Assert.AreEqual(2, result.Children.Count);
            Assert.AreEqual("agentdesk", result.Children[0].Identifier);
            Assert.AreEqual("workingdesk", result.Children[1].Identifier);
        });

        [TestMethod]
        public Task QuerySingle_WithAutoMultiMap_MultipleNestedPaths() => base.ExecuteTest(accessor =>
        {
            const string commandText = @"SELECT [x].[name], [y].[name], [z].[name]
FROM (VALUES (N'feature1')) AS [x]([name])
INNER JOIN (VALUES (N'black', N'feature1')
                 , (N'red', N'feature1')) AS [y]([name], [name_feature]) ON [x].[name] = [y].[name_feature]
INNER JOIN (VALUES (N'dependentfeaturex', N'feature1')
                 , (N'dependentfeaturey', N'feature1')) AS [z]([name], [name_feature]) ON [x].[name] = [z].[name_feature]";
            FeatureEntity result = accessor.QuerySingle<FeatureEntity>(commandText, CommandType.Text, accessor.Parameters().Build(), new[] { typeof(FeatureEntity), typeof(FeatureItemEntity), typeof(DependentFeatureEntity) }, "name,name");
            Assert.AreEqual("feature1", result.Name);
            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual("black", result.Items[0].Name);
            Assert.AreEqual("red", result.Items[1].Name);
            Assert.AreEqual(2, result.Dependencies.Count);
            Assert.AreEqual("dependentfeaturex", result.Dependencies[0].Name);
            Assert.AreEqual("dependentfeaturey", result.Dependencies[1].Name);
        });

        [TestMethod]
        public Task QuerySingle_WithAutoMultiMap_SingleDuplicateProperty_SetOnlyOnce() => base.ExecuteTest(accessor =>
        {
            const string commandText = @"SELECT [x].[name], [y].[name], [z].[name]
FROM (VALUES (N'feature1')) AS [x]([name])
INNER JOIN (VALUES (N'feature1_base', N'feature1')) AS [y]([name], [name_feature]) ON [x].[name] = [y].[name_feature]
INNER JOIN (VALUES (N'black', N'feature1')
                 , (N'red', N'feature1')) AS [z]([name], [name_feature]) ON [x].[name] = [z].[name_feature]";
            FeatureEntity result = accessor.QuerySingle<FeatureEntity>(commandText, CommandType.Text, accessor.Parameters().Build(), new[] { typeof(FeatureEntity), typeof(FeatureBaseEntity), typeof(FeatureItemEntity) }, "name,name");
            Assert.AreEqual("feature1", result.Name);
            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual("black", result.Items[0].Name);
            Assert.AreEqual("red", result.Items[1].Name);
            Assert.IsNotNull(result.Base);
            Assert.AreEqual("feature1_base", result.Base.Name);
        });

        [TestMethod]
        public Task QuerySingle_WithAutoMultiMap_AmbiguousKey_IdentifiedUsingParent() => base.ExecuteTest(accessor =>
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
            FeatureEntity result = accessor.QuerySingle<FeatureEntity>(commandText, CommandType.Text, accessor.Parameters().Build(), new[] { typeof(FeatureEntity), typeof(TranslationEntity), typeof(FeatureItemEntity), typeof(TranslationEntity) }, "lcid,name,lcid");
            Assert.AreEqual("feature1", result.Name);
            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual("black", result.Items[0].Name);
            Assert.AreEqual("red", result.Items[1].Name);
            Assert.AreEqual(2, result.Translations.Count);
            Assert.AreEqual(7, result.Translations[0].Lcid);
            Assert.AreEqual("feature_de", result.Translations[0].Value);
            Assert.AreEqual(9, result.Translations[1].Lcid);
            Assert.AreEqual("feature_en", result.Translations[1].Value);
            Assert.AreEqual(2, result.Items[0].Translations.Count);
            Assert.AreEqual(7, result.Items[0].Translations[0].Lcid);
            Assert.AreEqual("black_de", result.Items[0].Translations[0].Value);
            Assert.AreEqual(9, result.Items[0].Translations[1].Lcid);
            Assert.AreEqual("black_en", result.Items[0].Translations[1].Value);
            Assert.AreEqual(2, result.Items[1].Translations.Count);
            Assert.AreEqual(7, result.Items[1].Translations[0].Lcid);
            Assert.AreEqual("red_de", result.Items[1].Translations[0].Value);
            Assert.AreEqual(9, result.Items[1].Translations[1].Lcid);
            Assert.AreEqual("red_en", result.Items[1].Translations[1].Value);
        });

        [TestMethod]
        public Task QuerySingle_WithAutoMultiMap_NonEntityNonPrimitive_Collection_ReferenceEqualityIsPerformed() => base.ExecuteTest(accessor =>
        {
            const string commandText = @"SELECT [x].[name], [y].[name], [z].[data]
FROM (VALUES (N'feature1')) AS [x]([name])
INNER JOIN (VALUES (N'black', N'feature1')
                 , (N'red', N'feature1')) AS [y]([name], [name_feature]) ON [x].[name] = [y].[name_feature]
INNER JOIN (VALUES (0x1, N'feature1')
                 , (0x2, N'feature1')) AS [z]([data], [feature]) ON [x].[name] = [z].[feature]";
            FeatureEntity result = accessor.QuerySingle<FeatureEntity>(commandText, CommandType.Text, accessor.Parameters().Build(), new[] { typeof(FeatureEntity), typeof(FeatureItemEntity), typeof(byte[]) }, "name,data");
            Assert.AreEqual("feature1", result.Name);
            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual("black", result.Items[0].Name);
            Assert.AreEqual("red", result.Items[1].Name);
            Assert.AreEqual(2, result.Pictures.Count);
            Assert.IsTrue(result.Pictures[0].SequenceEqual(Enumerable.Repeat((byte)1, 1)));
            Assert.IsTrue(result.Pictures[1].SequenceEqual(Enumerable.Repeat((byte)2, 1)));
        });

        [TestMethod]
        public Task QuerySingle_WithAutoMultiMap_NonEntityNonPrimitive_SingleProperty_ReferenceEqualityIsPerformed() => base.ExecuteTest(accessor =>
        {
            const string commandText = @"SELECT [x].[name], [y].[data], [z].[name]
FROM (VALUES (N'product1')) AS [x]([name])
INNER JOIN (VALUES (0x1, N'product1')
                 , (0x2, N'product1')) AS [y]([data], [product]) ON [x].[name] = [y].[product]
INNER JOIN (VALUES (N'feature1', N'product1')
                 , (N'feature2', N'product1')) AS [z]([name], [name_product]) ON [x].[name] = [z].[name_product]";
            ProductEntity result = accessor.QuerySingle<ProductEntity>(commandText, CommandType.Text, accessor.Parameters().Build(), new[] { typeof(ProductEntity), typeof(byte[]), typeof(FeatureEntity) }, "data,name");
            Assert.AreEqual("product1", result.Name);
            Assert.IsTrue(result.Picture.SequenceEqual(Enumerable.Repeat((byte)1, 1)));
            Assert.AreEqual(2, result.Features.Count);
            Assert.AreEqual("feature1", result.Features[0].Name);
            Assert.AreEqual("feature2", result.Features[1].Name);
        });

        [TestMethod]
        public Task QuerySingle_WithAutoMultiMap_Primitive_Collection_ReferenceEqualityIsPerformed() => base.ExecuteTest(accessor =>
        {
            const string commandText = @"SELECT [name] = [x].[feature_name], [name] = [y].[featureitem_name], [z].[featureitem_id]
FROM (VALUES (N'feature1')) AS [x]([feature_name])
INNER JOIN (VALUES (N'black', N'feature1')
                 , (N'red', N'feature1')) AS [y]([featureitem_name], [feature_name]) ON [x].[feature_name] = [y].[feature_name]
INNER JOIN (VALUES (1, N'feature1')
                 , (2, N'feature1')) AS [z]([featureitem_id], [feature_name]) ON [x].[feature_name] = [z].[feature_name]";
            FeatureEntity result = accessor.QuerySingle<FeatureEntity>(commandText, CommandType.Text, accessor.Parameters().Build(), new[] { typeof(FeatureEntity), typeof(FeatureItemEntity), typeof(int) }, "name,featureitem_id");
            Assert.AreEqual("feature1", result.Name);
            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual("black", result.Items[0].Name);
            Assert.AreEqual("red", result.Items[1].Name);
            Assert.AreEqual(2, result.FeatureItemIds.Count);
            Assert.AreEqual(1, result.FeatureItemIds[0]);
            Assert.AreEqual(2, result.FeatureItemIds[1]);
        });

        [TestMethod]
        public Task QuerySingle_WithAutoMultiMap_Primitive_SingleProperty_NoMappingIsPerformed() => base.ExecuteTest(accessor =>
        {
            const string commandText = @"SELECT [name] = [x].[product_name], [y].[feature_id], [name] = [z].[feature_name]
FROM (VALUES (N'product1')) AS [x]([product_name])
INNER JOIN (VALUES (1, N'product1')
                 , (2, N'product1')) AS [y]([feature_id], [product_name]) ON [x].[product_name] = [y].[product_name]
INNER JOIN (VALUES (N'feature1', N'product1')
                 , (N'feature2', N'product1')) AS [z]([feature_name], [product_name]) ON [x].[product_name] = [z].[product_name]";
            ProductEntity result = accessor.QuerySingle<ProductEntity>(commandText, CommandType.Text, accessor.Parameters().Build(), new[] { typeof(ProductEntity), typeof(int), typeof(FeatureEntity) }, "feature_id,name");
            Assert.AreEqual("product1", result.Name);
            Assert.AreEqual(0, result.FirstFeatureId); // Setting a primitive property using multi map doesn't make sense and is therefore not supported
            Assert.AreEqual(2, result.Features.Count);
            Assert.AreEqual("feature1", result.Features[0].Name);
            Assert.AreEqual("feature2", result.Features[1].Name);
        });

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
            public IList<int> FeatureItemIds { get; }

            public FeatureEntity()
            {
                this.Translations = new Collection<TranslationEntity>();
                this.Items = new Collection<FeatureItemEntity>();
                this.Dependencies = new Collection<DependentFeatureEntity>();
                this.Pictures = new Collection<byte[]>();
                this.FeatureItemIds = new Collection<int>();
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
            public int FirstFeatureId { get; set; }

            public ProductEntity()
            {
                this.Features = new Collection<FeatureEntity>();
            }
        }
    }
}