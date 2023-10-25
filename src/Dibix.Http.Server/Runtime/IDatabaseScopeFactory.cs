namespace Dibix.Http.Server
{
    public interface IDatabaseScopeFactory
    {
        IDatabaseAccessorFactory Create<TInitiator>();
        IDatabaseAccessorFactory Create(string initiatorFullName);
    }
}