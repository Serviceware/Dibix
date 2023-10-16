using System;

namespace Dibix.Http.Host
{
    public interface IEndpointImplementationProvider
    {
        Delegate GetImplementation(EndpointDefinition endpoint);
    }
}