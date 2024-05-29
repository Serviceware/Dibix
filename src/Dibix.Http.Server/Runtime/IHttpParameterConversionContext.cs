using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    public interface IHttpParameterConversionContext
    {
        Expression RequestParameter { get; }
        Expression DependencyResolverParameter { get; }
        Expression ActionParameter { get; }
        
        void AppendRequiredClaim(string claimType);
    }
}