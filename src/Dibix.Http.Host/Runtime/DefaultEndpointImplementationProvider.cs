using System;
using Dibix.Http.Server;

namespace Dibix.Http.Host
{
    internal sealed class DefaultEndpointImplementationProvider : IEndpointImplementationProvider
    {
        public Delegate GetImplementation(EndpointDefinition endpoint)
        {
            Delegate @delegate = endpoint.ActionDefinition.Delegate;
            if (@delegate == null)
                throw new InvalidOperationException($"Endpoint has no delegate: {endpoint.Method} {endpoint.ActionDefinition.Uri}");

            return @delegate;
        }
    }
}