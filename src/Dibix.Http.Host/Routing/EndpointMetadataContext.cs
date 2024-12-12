using System;
using Dibix.Http.Server.AspNetCore;

namespace Dibix.Http.Host
{
    internal sealed class EndpointMetadataContext
    {
        private EndpointDefinition? _value;

        public EndpointDefinition Value => _value ?? throw new InvalidOperationException("Endpoint metadata not initialized or not available");

        public void Initialize(EndpointDefinition endpointDefinition) => _value = endpointDefinition;
    }
}