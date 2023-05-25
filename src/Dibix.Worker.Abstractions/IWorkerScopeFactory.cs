namespace Dibix.Worker.Abstractions
{
    public interface IWorkerScopeFactory
    {
        IWorkerScope Create<TInitiator>();
        IWorkerScope Create(string initiatorFullName);
    }
}