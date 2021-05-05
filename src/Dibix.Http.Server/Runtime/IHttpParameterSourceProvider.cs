namespace Dibix.Http.Server
{
    public interface IHttpParameterSourceProvider
    {
        HttpParameterLocation Location { get; }

        void Resolve(IHttpParameterResolutionContext context);
    }
}