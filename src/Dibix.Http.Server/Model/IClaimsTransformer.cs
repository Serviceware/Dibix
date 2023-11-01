using System.Security.Claims;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public interface IClaimsTransformer
    {
        Task TransformAsync(ClaimsPrincipal claimsPrincipal);
    }
}