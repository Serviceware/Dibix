using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Http
{
    public sealed class HttpControllerDefinition
    {
        public string AreaName { get; }
        public string ControllerName { get; }

        public ICollection<HttpActionDefinition> Actions { get; }
        public ICollection<Type> ControllerImports { get; }

        internal HttpControllerDefinition(string areaName, string controllerName)
        {
            this.AreaName = areaName;
            this.ControllerName = controllerName;
            this.Actions = new Collection<HttpActionDefinition>();
            this.ControllerImports = new Collection<Type>();
        }

        public void AddAction(IHttpActionTarget target, Action<HttpActionDefinition> setupAction)
        {
            HttpActionDefinition action = new HttpActionDefinition(this, target);
            setupAction(action);
            this.Actions.Add(action);
        }
    }
}