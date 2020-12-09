using System.Linq.Expressions;

namespace Dibix.Http
{
    internal abstract class ArgumentsSourceProvider : IHttpParameterSourceProvider
    {
        public void Resolve(IHttpParameterResolutionContext context)
        {
            Expression value = HttpParameterResolverUtility.BuildArgumentAccessorExpression(context.ArgumentsParameter, context.PropertyPath);
            context.ResolveUsingValue(value);
        }
    }
}