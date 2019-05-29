using System;

namespace Dibix
{
    internal sealed class EntityProperty
    {
        private readonly Action<object, object> _valueSetter;

        public string Name { get; }
        public Type EntityType { get; }
        public bool IsCollection { get; }

        public EntityProperty(string name, Type entityType, bool isCollection, Action<object, object> valueSetter)
        {
            this.Name = name;
            this.EntityType = entityType;
            this.IsCollection = isCollection;
            this._valueSetter = valueSetter;
        }

        public void SetValue(object instance, object value) => this._valueSetter(instance, value);
    }
}