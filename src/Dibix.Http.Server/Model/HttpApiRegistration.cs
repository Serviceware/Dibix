using System.Reflection;

namespace Dibix.Http.Server
{
    internal sealed class HttpApiRegistration : HttpApiDescriptor
    {
        public Assembly Assembly { get; }

        public HttpApiRegistration(Assembly assembly) => this.Assembly = assembly;

        public override void Configure(IHttpApiDiscoveryContext context) { }

        protected override string ResolveAreaName(Assembly assembly) => base.ResolveAreaName(this.Assembly);
    }
}