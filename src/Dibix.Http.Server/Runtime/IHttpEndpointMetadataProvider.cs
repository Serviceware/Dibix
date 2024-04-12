namespace Dibix.Http.Server
{
    public interface IHttpEndpointMetadataProvider
    {
        HttpActionDefinition GetActionDefinition();
    }
}
