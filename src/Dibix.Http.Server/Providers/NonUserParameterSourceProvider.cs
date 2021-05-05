namespace Dibix.Http.Server
{
    public abstract class NonUserParameterSourceProvider : IHttpParameterSourceProvider
    {
        public virtual HttpParameterLocation Location => HttpParameterLocation.NonUser;

        public abstract void Resolve(IHttpParameterResolutionContext context);
    }
}