using System;
using System.Collections.Generic;

namespace Dibix.Http
{
    public sealed class HttpActionDefinition
    {
        private Uri _computedUri;

        public HttpControllerDefinition Controller { get; }
        public IHttpActionTarget Target { get; }
        public HttpApiMethod Method { get; set; }
        public string ChildRoute { get; set; }
        public bool OmitResult { get; set; }
        public bool IsAnonymous { get; set; }
        public string Description { get; set; }
        public IDictionary<string, HttpParameterSource> DynamicParameters { get; }
        public Uri ComputedUri => this._computedUri ?? (this._computedUri = this.BuildUri());

        internal HttpActionDefinition(HttpControllerDefinition controller, IHttpActionTarget target)
        {
            this.Controller = controller;
            this.Target = target;
            this.DynamicParameters = new Dictionary<string, HttpParameterSource>(StringComparer.OrdinalIgnoreCase);
        }

        public void ResolveParameter(string targetParameterName, bool value)
        {
            this.ResolveParameter(targetParameterName, new HttpParameterConstantSource(value));
        }
        public void ResolveParameter(string targetParameterName, string sourceName, string sourcePropertyName)
        {
            this.ResolveParameter(targetParameterName, new HttpParameterPropertySource(sourceName, sourcePropertyName));
        }

        private Uri BuildUri()
        {
            return new Uri(RouteBuilder.BuildRoute(this.Controller.AreaName, this.Controller.ControllerName, this.ChildRoute), UriKind.Relative);
        }

        private void ResolveParameter(string targetParameterName, HttpParameterSource source)
        {
            this.DynamicParameters.Add(targetParameterName, source);
        }
    }
}