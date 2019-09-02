namespace Dibix.Http
{
    public interface IParameterDependencyResolver
    {
        T Resolve<T>();
    }
}
