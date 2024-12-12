using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Dibix.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Host
{
    internal sealed class EndpointAuthorizationHandlerContextFactory : IAuthorizationHandlerContextFactory
    {
        private readonly IAuthorizationHandlerContextFactory _implementation = new DefaultAuthorizationHandlerContextFactory();

        public AuthorizationHandlerContext CreateContext(IEnumerable<IAuthorizationRequirement> requirements, ClaimsPrincipal user, object? resource)
        {
            IEnumerable<IAuthorizationRequirement> resolvedRequirements = requirements;
            if (resource is HttpContext httpContext)
            {
                EndpointDefinition? endpointDefinition = httpContext.TryGetEndpointDefinition();
                if (endpointDefinition != null)
                {
                    resolvedRequirements = resolvedRequirements.Concat(endpointDefinition.ActionDefinition.RequiredClaims.Select(x => new ClaimsAuthorizationRequirement(x, allowedValues: null)))
                                                               .ToArray();
                }
            }

            return _implementation.CreateContext(resolvedRequirements, user, resource);
        }
    }
}