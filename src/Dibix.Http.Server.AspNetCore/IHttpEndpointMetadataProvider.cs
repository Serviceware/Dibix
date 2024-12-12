namespace Dibix.Http.Server.AspNetCore
{
    public interface IHttpEndpointMetadataProvider
    {
        HttpActionDefinition GetActionDefinition();
    }
}
