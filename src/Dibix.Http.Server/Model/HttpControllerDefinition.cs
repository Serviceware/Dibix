using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Http.Server
{
    public sealed class HttpControllerDefinition
    {
        private string _productName;
        private string _fullName;

        public string ProductName
        {
            // Not supported on all platforms: Dibix.Http.Host yes, Dibix.Http.Server no
            get => _productName ?? throw new InvalidOperationException($"{nameof(ProductName)} is not initialized");
            set => _productName = value;
        }
        public string AreaName { get; set; }
        public string ControllerName { get; }
        public string FullName => _fullName ??= $"{ProductName}.{AreaName}";
        public IReadOnlyCollection<HttpActionDefinition> Actions { get; }
        public IReadOnlyCollection<string> ControllerImports { get; }

        internal HttpControllerDefinition(string productName, string areaName, string controllerName, IList<HttpActionDefinition> actions, IList<string> imports)
        {
            ProductName = productName;
            AreaName = areaName;
            ControllerName = controllerName;
            Actions = new ReadOnlyCollection<HttpActionDefinition>(actions);
            ControllerImports = new ReadOnlyCollection<string>(imports);
        }
    }
}