using System;

namespace Dibix.Http
{
    public sealed class HttpActionDefinition : HttpParameterSourceSelector
    {
        private Uri _computedUri;

        public HttpControllerDefinition Controller { get; }
        public IHttpActionTarget Target { get; }
        public HttpApiMethod Method { get; set; }
        public string ChildRoute { get; set; }
        public Type BodyContract { get; set; }
        public Type BodyBinder { get; set; }
        public bool IsAnonymous { get; set; }
        public HttpFileResponseDefinition FileResponse { get; set; }
        public string Description { get; set; }
        public Uri ComputedUri => this._computedUri ?? (this._computedUri = this.BuildUri());

        internal HttpActionDefinition(HttpControllerDefinition controller, IHttpActionTarget target)
        {
            this.Controller = controller;
            this.Target = target;
        }

        public void ResolveParameterFromBody(string targetParameterName, string bodyConverterName)
        {
            base.ResolveParameter(targetParameterName, new HttpParameterBodySource(bodyConverterName));
        }
        public void ResolveParameterFromSource(string targetParameterName, string sourceName, string sourcePropertyName, Action<IHttpParameterSourceSelector> itemSources)
        {
            HttpParameterPropertySource source = base.ResolveParameterFromSourceCore(targetParameterName, sourceName, sourcePropertyName, null);
            if (itemSources == null) 
                return;

            HttpParameterSourceSelector sourceSelector = new HttpParameterSourceSelector();
            itemSources.Invoke(sourceSelector);
            source.ItemSources.AddRange(sourceSelector.ParameterSources);
        }

        private Uri BuildUri()
        {
            return new Uri(RouteBuilder.BuildRoute(this.Controller.AreaName, this.Controller.ControllerName, this.ChildRoute), UriKind.Relative);
        }
    }
}