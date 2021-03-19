namespace Dibix.Http
{
    public interface IHttpParameterSourceProvider
    {
        void Resolve(IHttpParameterResolutionContext context);
    }
}