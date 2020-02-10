using System;

namespace Dibix
{
    internal sealed class EntityProperty
    {
        private readonly Func<object, object> _valueGetter;
        private readonly Action<object, object> _valueSetter;

        public string Name { get; }
        public Type EntityType { get; }
        public bool IsCollection { get; }

        public EntityProperty(string name, Type entityType, bool isCollection, Func<object, object> valueGetter, Action<object, object> valueSetter)
        {
            this.Name = name;
            this.EntityType = entityType;
            this.IsCollection = isCollection;
            this._valueGetter = valueGetter;
            this._valueSetter = valueSetter;
        }

        public object GetValue(object instance)
        {
            if (this._valueGetter == null)
                throw new InvalidOperationException("Getting the value on this instance is not supported");

            return this._valueGetter(instance);
        }

        public void SetValue(object instance, object value) => this._valueSetter(instance, value);
    }
}