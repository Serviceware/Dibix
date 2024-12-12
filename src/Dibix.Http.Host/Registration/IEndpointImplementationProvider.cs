using System;
using Dibix.Http.Server.AspNetCore;

namespace Dibix.Http.Host
{
    public interface IEndpointImplementationProvider
    {
        Delegate GetImplementation(EndpointDefinition endpoint);
    }
}