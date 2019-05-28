using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
                MultiMapper multiMapper = new MultiMapper();
                RecursiveEntity result = accessor.QueryMany<RecursiveEntity, RecursiveEntity, RecursiveEntity>(commandText, CommandType.Text, accessor.Parameters().Build(), (a, b) =>
                {
                    multiMapper.MultiMap(a, b);
                    return a;
                }, "identifier").Distinct(new EntityComparer<RecursiveEntity>()).Single();
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
                MultiMapper multiMapper = new MultiMapper();
                FeatureEntity result = accessor.QueryMany<FeatureEntity, FeatureItemEntity, DependentFeatureEntity, FeatureEntity>(commandText, CommandType.Text, accessor.Parameters().Build(), (a, b, c) =>
                {
                    multiMapper.MultiMap(a, b, c);
                    return a;
                }, "name,name").Distinct(new EntityComparer<FeatureEntity>()).Single();
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
            public ICollection<FeatureItemEntity> Items { get; }
            public ICollection<DependentFeatureEntity> Dependencies { get; }

            public FeatureEntity()
            {
                this.Items = new Collection<FeatureItemEntity>();
                this.Dependencies = new Collection<DependentFeatureEntity>();
            }
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

    internal static class EntityDescriptorCache
    {
        private static readonly ConcurrentDictionary<Type, EntityDescriptor> Cache = new ConcurrentDictionary<Type, EntityDescriptor>();

        public static EntityDescriptor GetDescriptor(Type type) => Cache.GetOrAdd(type, BuildDescriptor);

        private static EntityDescriptor BuildDescriptor(Type type)
        {
            EntityDescriptor descriptor = new EntityDescriptor();
            descriptor.Keys.AddRange(type.GetProperties()
                                         .Where(x => x.IsDefined(typeof(KeyAttribute)))
                                         .Select(BuildEntityKey));
            return descriptor;
        }

        private static EntityKey BuildEntityKey(PropertyInfo property)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
            Expression instanceCast = Expression.Convert(instanceParameter, property.DeclaringType);
            Expression propertyAccessor = Expression.Property(instanceCast, property);
            Expression<Func<object, object>> expression = Expression.Lambda<Func<object, object>>(propertyAccessor, instanceParameter);
            Func<object, object> compiled = expression.Compile();
            return new EntityKey(compiled);
        }
    }

    internal sealed class EntityDescriptor
    {
        public ICollection<EntityKey> Keys { get; }

        public EntityDescriptor()
        {
            this.Keys = new Collection<EntityKey>();
        }
    }

    internal sealed class EntityKey
    {
        private readonly Func<object, object> _valueAccessor;

        public EntityKey(Func<object, object> valueAccessor)
        {
            this._valueAccessor = valueAccessor;
        }

        public object GetValue(object instance) => this._valueAccessor(instance);
    }

    internal class MultiMapper
    {
        private struct CollectionEntry
        {
            public object Instance { get; set; }
            public object Collection { get; set; }
        }

        private readonly IDictionary<object, object> _entityCache = new Dictionary<object, object>(new EntityComparer());
        private readonly IDictionary<CollectionEntry, HashSet<object>> _collectionCache = new Dictionary<CollectionEntry, HashSet<object>>();

        private object GetCachedEntity(object item)
        {
            if (!this._entityCache.TryGetValue(item, out object existingValue))
            {
                existingValue = item;
                this._entityCache.Add(item, item);
            }
            return existingValue;
        }


        public void MultiMap(params object[] args)
        {
            object item1 = args[0];

            for (int i = args.Length - 1; i > 0; i--)
            {
                object item = GetCachedEntity(args[i]);

                for (int j = i - 1; j >= 0; j--)
                {
                    object previousItem = GetCachedEntity(args[j]);
                    PropertyAccessor property = TypeAccessor.GetProperties(previousItem.GetType()).FirstOrDefault(x => MatchType(x, item.GetType()));
                    if (property == null)
                        continue;

                    Type genericType = typeof(ICollection<>).MakeGenericType(item.GetType());
                    if (genericType.IsAssignableFrom(property.Type))
                    {
                        if (InCollection(property, previousItem, item))
                            break;

                        object collection = property.GetValue(previousItem);
                        genericType.GetMethod("Add").Invoke(collection, new [] { item });
                    }
                    else
                    {
                        if (property.GetValue(previousItem) != null)
                            break;

                        property.SetValue(previousItem, item);
                    }
                    break;
                }
            }
        }

        private bool InCollection(PropertyAccessor property, object instance, object item)
        {
            CollectionEntry key = new CollectionEntry { Instance = instance, Collection = property };
            if (!this._collectionCache.TryGetValue(key, out HashSet<object> collection))
            {
                collection = new HashSet<object>(new EntityComparer());
                this._collectionCache.Add(key, collection);
            }

            if (!collection.Contains(item))
            {
                collection.Add(item);
                return false;
            }

            return true;
        }

        private static bool MatchType(PropertyAccessor property, Type targetType)
        {
            if (property.Type == targetType)
                return true;

            if (typeof(ICollection<>).MakeGenericType(targetType).IsAssignableFrom(property.Type))
                return true;

            return false;
        }
    }

    internal sealed class EntityComparer<T> : IEqualityComparer<T>
    {
        private readonly IEqualityComparer<object> _inner;

        public EntityComparer()
        {
            this._inner = new EntityComparer();
        }

        bool IEqualityComparer<T>.Equals(T x, T y) => this._inner.Equals(x, y);

        int IEqualityComparer<T>.GetHashCode(T obj) => this._inner.GetHashCode(obj);
    }

    internal sealed class EntityComparer : IEqualityComparer<object>
    {
        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            if (x.GetType() != y.GetType())
                return false;

            EntityDescriptor entityDescriptor = EntityDescriptorCache.GetDescriptor(x.GetType());
            return entityDescriptor.Keys.All(a => Equals(a.GetValue(x), a.GetValue(y)));
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            EntityDescriptor entityDescriptor = EntityDescriptorCache.GetDescriptor(obj.GetType());
            int hashCode = entityDescriptor.Keys
                                           .Select(x => x.GetValue(obj).GetHashCode())
                                           .Aggregate((x, y) => x ^ y);
            return hashCode;
        }
    }
}
