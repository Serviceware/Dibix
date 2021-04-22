namespace Dibix.Testing.Http
{
    public interface IHttpTestServiceBuilder<out TService>
    {
        TService Build();
    }
}