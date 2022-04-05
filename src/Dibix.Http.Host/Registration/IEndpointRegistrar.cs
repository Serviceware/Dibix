using Microsoft.AspNetCore.Routing;

namespace Dibix.Http.Host
{
    public interface IEndpointRegistrar
    {
        void Register(IEndpointRouteBuilder builder);
    }
}