using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Dibix.Http
{
    public abstract class HttpApiDescriptor
    {
        #region Properties
        public string AreaName { get; }
        public ICollection<HttpControllerDefinition> Controllers { get; }
        #endregion

        #region Constructor
        protected HttpApiDescriptor()
        {
            this.Controllers = new Collection<HttpControllerDefinition>();

            Type implType = this.GetType();
            Assembly assembly = implType.GetTypeInfo().Assembly;
            ApiRegistrationAttribute attribute = assembly.GetCustomAttribute<ApiRegistrationAttribute>();
            if (attribute == null)
                throw new InvalidOperationException($"Assembly {assembly.GetName().Name} is not marked with {typeof(ApiRegistrationAttribute)}");

            if (String.IsNullOrEmpty(attribute.AreaName))
                throw new InvalidOperationException($@"Area name cannot be empty
{assembly.GetName().Name} -> {implType}");

            this.AreaName = attribute.AreaName;
        }
        #endregion

        #region Abstract Methods
        public abstract void Configure();
        #endregion

        #region Protected Methods
        protected void RegisterController(string controllerName, Action<HttpControllerDefinition> setupAction)
        {
            HttpControllerDefinition controller = new HttpControllerDefinition(this.AreaName, controllerName);
            setupAction(controller);
            this.Controllers.Add(controller);
        }
        #endregion
    }
}
