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
        private readonly ICollection<IClaimsTransformer> _claimsTransformers;

        public ComposableClaimsTransformation(IEnumerable<IClaimsTransformer> claimsTransformers)
        {
            _claimsTransformers = claimsTransformers.ToArray();
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            foreach (IClaimsTransformer claimsTransformer in _claimsTransformers)
            {
                await claimsTransformer.TransformAsync(principal).ConfigureAwait(false);
            }
            return principal;
        }
    }
}