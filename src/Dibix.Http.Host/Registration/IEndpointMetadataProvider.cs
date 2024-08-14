﻿using System.Collections.Generic;
using Dibix.Http.Server;

namespace Dibix.Http.Host
{
    public interface IEndpointMetadataProvider
    {
        IEnumerable<EndpointDefinition> GetEndpoints();
    }
}