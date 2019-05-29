using System;

namespace Dibix
{
    internal sealed class EntityKey
    {
        private readonly Func<object, object> _valueAccessor;

        public EntityKey(Func<object, object> valueAccessor)
        {
            this._valueAccessor = valueAccessor;
        }

        public object GetValue(object instance) => this._valueAccessor(instance);
    }
}