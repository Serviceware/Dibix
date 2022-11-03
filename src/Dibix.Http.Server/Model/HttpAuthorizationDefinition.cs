namespace Dibix.Http.Server
{
    public sealed class HttpAuthorizationDefinition : IHttpActionExecutionDefinition
    {
        public IHttpActionExecutionMethod Executor { get; set; }
        public IHttpParameterResolutionMethod ParameterResolver { get; set; }
    }
}