namespace Dibix.Http.Server
{
    public interface IHttpParameterSourceProvider
    {
        void Resolve(IHttpParameterResolutionContext context);
    }
}