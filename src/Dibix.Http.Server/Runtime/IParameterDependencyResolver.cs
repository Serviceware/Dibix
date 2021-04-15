namespace Dibix.Http.Server
{
    public interface IParameterDependencyResolver
    {
        T Resolve<T>();
    }
}
