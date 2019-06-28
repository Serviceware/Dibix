﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal sealed class MultiMapper
    {
        #region Fields
        private readonly HashCollection<object> _entityCache = new HashCollection<object>(new EntityComparer());
        private readonly IDictionary<InstancePropertyKey, HashSet<object>> _collectionCache = new Dictionary<InstancePropertyKey, HashSet<object>>();
        #endregion

        #region Public Methods
        public TReturn AutoMap<TReturn>(bool useProjection, params object[] args) where TReturn : new()
        {
            // LEFT JOIN => related entities can be null
            object[] relevantArgs = args.Where(x => x != null).ToArray();

            for (int i = relevantArgs.Length - 1; i > 0; i--)
            {
                object item = this.GetCachedEntity(relevantArgs[i]);

                for (int j = i - 1; j >= 0; j--)
                {
                    object previousItem = this.GetCachedEntity(relevantArgs[j]);
                    EntityDescriptor descriptor = EntityDescriptorCache.GetDescriptor(previousItem.GetType());
                    EntityProperty property = descriptor.ComplexProperties.FirstOrDefault(x => MatchProperty(x, item.GetType()));
                    if (property == null)
                        continue;

                    if (property.IsCollection && this.IsInCollection(previousItem, property.Name, item))
                        break;

                    property.SetValue(previousItem, item);
                    break;
                }
            }

            if (!useProjection)
                return (TReturn)args[0];

            return ProjectResult<TReturn>(relevantArgs);
        }
        #endregion

        #region Private Methods
        private object GetCachedEntity(object item)
        {
            if (!this._entityCache.TryGetValue(item, out object existingValue))
            {
                existingValue = item;
                this._entityCache.Add(item);
            }
            return existingValue;
        }

        private bool IsInCollection(object instance, string property, object item)
        {
            InstancePropertyKey key = new InstancePropertyKey(instance, property);
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

        private static bool MatchProperty(EntityProperty entityProperty, Type targetType)
        {
            return entityProperty.EntityType == targetType;
        }

        private static TReturn ProjectResult<TReturn>(params object[] args) where TReturn : new()
        {
            TReturn result = new TReturn();
            return result;
        }
        #endregion

        #region Nested Types
        private struct InstancePropertyKey
        {
            public object Instance { get; }
            public string Property { get; }

            public InstancePropertyKey(object instance, string property)
            {
                this.Instance = instance;
                this.Property = property;
            }
        }
        #endregion
    }
}