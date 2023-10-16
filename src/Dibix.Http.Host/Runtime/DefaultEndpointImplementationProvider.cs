using System;

namespace Dibix.Http.Host
{
    internal sealed class DefaultEndpointImplementationProvider : IEndpointImplementationProvider
    {
        public Delegate GetImplementation(EndpointDefinition endpoint) => endpoint.ActionDefinition.Delegate;
    }
}