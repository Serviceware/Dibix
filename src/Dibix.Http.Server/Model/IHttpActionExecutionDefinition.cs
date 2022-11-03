namespace Dibix.Http.Server
{
    public interface IHttpActionExecutionDefinition
    {
        IHttpActionExecutionMethod Executor { get; }
        IHttpParameterResolutionMethod ParameterResolver { get; }
    }
}