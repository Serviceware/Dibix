using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Authentication;

namespace Dibix.Http.Host
{
    internal sealed class ComposableClaimsTransformation : IClaimsTransformation
    {
        private readonly EndpointMetadataContext _endpointMetadataContext;
        private readonly ICollection<IClaimsTransformer> _claimsTransformers;

        public ComposableClaimsTransformation(EndpointMetadataContext endpointMetadataContext, IEnumerable<IClaimsTransformer> claimsTransformers)
        {
            _endpointMetadataContext = endpointMetadataContext;
            _claimsTransformers = claimsTransformers.ToArray();
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Only apply for Dibix endpoints
            if (_endpointMetadataContext.IsInitialized)
            {
                foreach (IClaimsTransformer claimsTransformer in _claimsTransformers)
                {
                    await claimsTransformer.TransformAsync(principal).ConfigureAwait(false);
                }
            }

            return principal;
        }
    }
}