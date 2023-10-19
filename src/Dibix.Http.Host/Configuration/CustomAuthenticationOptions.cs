using System;
using Dibix.Http.Server;

namespace Dibix.Http.Host
{
    public sealed class CustomAuthenticationOptions
    {
        public const string SchemeName = "Custom";
        public Func<HttpActionDefinition, bool> EndpointFilter { get; set; } = _ => false;
        
    }
}