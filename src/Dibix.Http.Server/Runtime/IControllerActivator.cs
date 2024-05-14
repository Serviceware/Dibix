namespace Dibix.Http.Server
{
    public interface IControllerActivator
    {
        TInstance CreateInstance<TInstance>();
    }
}