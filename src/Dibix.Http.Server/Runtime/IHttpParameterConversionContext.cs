using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    public interface IHttpParameterConversionContext
    {
        IHttpActionDescriptor Action { get; }
        Expression RequestParameter { get; }
        Expression DependencyResolverParameter { get; }
        Expression ActionParameter { get; }
    }
}