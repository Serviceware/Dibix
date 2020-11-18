using System.Linq.Expressions;

namespace Dibix.Http
{
    internal sealed class QueryParameterSourceProvider : IHttpParameterSourceProvider
    {
        public const string SourceName = "QUERY";

        public void Resolve(IHttpParameterResolutionContext context)
        {
            Expression value = HttpParameterResolverUtility.BuildArgumentAccessorExpression(context.ArgumentsParameter, context.PropertyPath);
            context.ResolveUsingValue(value);
        }
    }
}