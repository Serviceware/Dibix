using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Dibix.Testing
{
    public abstract class LazyConfiguration
    {
        private readonly ICollection<string> _visitedProperties;
        private string _propertyName;

        protected LazyConfiguration(string propertyName = null)
        {
            this._propertyName = propertyName;
            this._visitedProperties = new HashSet<string>();
        }

        protected virtual T GetProperty<T>(ref T storage, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, null))
            {
                if (!this._visitedProperties.Contains(propertyName)) // Property getter is called once by Microsoft.Extensions.Configuration.ConfigurationBinder.BindProperty
                    this._visitedProperties.Add(propertyName);
                else
                    throw new InvalidOperationException($"Property not configured: {this._propertyName}.{propertyName}");
            }

            return storage;
        }

        protected virtual void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null) where T : LazyConfiguration
        {
            value._propertyName = this.BuildPath(propertyName);
            storage = value;
        }

        private string BuildPath(string propertyName) => !String.IsNullOrEmpty(this._propertyName) ? $"{this._propertyName}.{propertyName}" : propertyName;
    }
}