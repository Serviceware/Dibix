namespace Dibix.Testing.Http
{
    public interface ITestAuthorizationContext
    {
        TService CreateService<TService>();
    }
}