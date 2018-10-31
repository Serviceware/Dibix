using System;
using System.Reflection;

namespace Dibix
{
    public sealed class PropertyAccessor
    {
        private readonly Func<object, object> _valueGetter;
        private readonly Action<object, object> _valueSetter;

        public string Name { get; }
        public Type Type { get; }

        private PropertyAccessor(string name, Type type, Func<object, object> valueGetter, Action<object, object> valueSetter)
        {
            this.Name = name;
            this.Type = type;
            this._valueGetter = valueGetter;
            this._valueSetter = valueSetter;
        }

        public static PropertyAccessor Create(PropertyInfo property)
        {
            Func<object, object> valueGetter = PropertyAccessorExpressionBuilder.BuildValueGetter(property);
            Action<object, object> valueSetter = PropertyAccessorExpressionBuilder.BuildValueSetter(property);
            PropertyAccessor accessor = new PropertyAccessor(property.Name, property.PropertyType, valueGetter, valueSetter);
            return accessor;
        }

        public object GetValue(object instance)
        {
            Guard.IsNotNull(instance, nameof(instance));
            return this._valueGetter(instance);
        }

        public void SetValue(object instance, object value)
        {
            Guard.IsNotNull(instance, nameof(instance));
            this._valueSetter(instance, value);
        }
    }
}
