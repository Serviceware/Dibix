using System;
using Dibix.Http.Server;

namespace Dibix.Http.Host
{
    public interface IEndpointImplementationProvider
    {
        Delegate GetImplementation(EndpointDefinition endpoint);
    }
}