using System;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Host
{
    public interface IEndpointImplementationProvider
    {
        Delegate GetImplementation(EndpointDefinition endpoint);
    }
}