using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Dibix.Http.Server
{
    public abstract class HttpApiDescriptor
    {
        #region Fields
        private readonly Lazy<string> _areaNameAccessor;
        #endregion

        #region Properties
        public string AreaName => this._areaNameAccessor.Value;
        public ICollection<HttpControllerDefinition> Controllers { get; }
        #endregion

        #region Constructor
        protected HttpApiDescriptor()
        {
            this.Controllers = new Collection<HttpControllerDefinition>();
            this._areaNameAccessor = new Lazy<string>(this.ResolveAreaName);
        }
        #endregion

        #region Abstract Methods
        public abstract void Configure(IHttpApiDiscoveryContext context);
        #endregion

        #region Protected Methods
        protected virtual string ResolveAreaName(Assembly assembly)
        {
            AreaRegistrationAttribute attribute = assembly.GetCustomAttribute<AreaRegistrationAttribute>();
            if (attribute == null)
                throw new InvalidOperationException($"Assembly {assembly.GetName().Name} is not marked with {typeof(AreaRegistrationAttribute)}");

            if (String.IsNullOrEmpty(attribute.AreaName))
                throw new InvalidOperationException($@"Area name cannot be empty
{assembly.GetName().Name} -> {this.GetType()}");

            return attribute.AreaName;
        }

        protected void RegisterController(string controllerName, Action<HttpControllerDefinition> setupAction)
        {
            Guard.IsNotNullOrEmpty(controllerName, nameof(controllerName));
            HttpControllerDefinition controller = new HttpControllerDefinition(this.AreaName, controllerName);
            setupAction(controller);
            this.Controllers.Add(controller);
        }
        #endregion

        #region Private Methods
        private string ResolveAreaName()
        {
            Assembly assembly = this.GetType().Assembly;
            return this.ResolveAreaName(assembly);
        }
        #endregion
    }
}