using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dibix
{
    internal sealed class MultiMapper : IPostProcessor
    {
        #region Fields
        private readonly HashCollection<object> _entityCache = new HashCollection<object>(EntityEqualityComparer.Instance);
        #endregion

        #region Public Methods
        public TReturn MapRow<TReturn>(bool useProjection, params object[] args) where TReturn : new()
        {
            // LEFT JOIN => related entities can be null
            object[] relevantArgs = args.Where(x => x != null).ToArray();

            PropertyMatcher propertyMatcher = new PropertyMatcher();
            for (int i = relevantArgs.Length - 1; i > 0; i--)
            {
                object item = relevantArgs[i].PostProcess();
                for (int j = i - 1; j >= 0; j--)
                {
                    object previousItem = this.GetCachedEntity(relevantArgs[j]);
                    EntityDescriptor descriptor = EntityDescriptorCache.GetDescriptor(previousItem.GetType());

                    if (!propertyMatcher.TryMatchProperty(descriptor, item.GetType(), out EntityProperty property))
                        continue;

                    if (!ShouldCollectValue(property, previousItem, item))
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

        #region IPostProcessor Members
        public IEnumerable<object> PostProcess(IEnumerable<object> source, Type type)
        {
            // Distinct because the root might be duplicated because of 1->n related rows
            IEnumerable<object> results = source.Distinct(EntityEqualityComparer.Instance);
            return results;
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

        private static bool ShouldCollectValue(EntityProperty property, object instance, object newValue)
        {
            object currentValue = property.GetValue(instance);
            return !property.IsCollection ? currentValue == null : !Contains(currentValue, newValue);
        }

        private static bool Contains(object collection, object item) => ((IEnumerable)collection).Cast<object>().Contains(item, EntityEqualityComparer.Instance);

        private static TReturn ProjectResult<TReturn>(params object[] args) where TReturn : new()
        {
            EntityDescriptor descriptor = EntityDescriptorCache.GetDescriptor(typeof(TReturn));
            TReturn result = new TReturn();
            PropertyMatcher propertyMatcher = new PropertyMatcher();
            foreach (object arg in args.Reverse())
            {
                if (!propertyMatcher.TryMatchProperty(descriptor, arg.GetType(), out EntityProperty property))
                    continue;

                property.SetValue(result, arg);
            }
            return result;
        }
        #endregion
    }
}