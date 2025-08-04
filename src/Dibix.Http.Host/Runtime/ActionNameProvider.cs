using Dibix.Http.Server;
using Dibix.Http.Server.AspNetCore;

namespace Dibix.Http.Host
{
    internal sealed class ActionNameProvider : IActionNameProvider
    {
        private readonly EndpointMetadataContext _endpointMetadataContext;

        public ActionNameProvider(EndpointMetadataContext endpointMetadataContext)
        {
            _endpointMetadataContext = endpointMetadataContext;
        }

        public string GetActionName()
        {
            return _endpointMetadataContext.ActionName;
        }
    }
}