using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    internal abstract class ArgumentsSourceProvider : NonUserParameterSourceProvider, IHttpParameterSourceProvider
    {
        public override void Resolve(IHttpParameterResolutionContext context)
        {
            Expression value = HttpParameterResolverUtility.BuildReadableArgumentAccessorExpression(context.ArgumentsParameter, context.PropertyPath);
            context.ResolveUsingValue(value);
        }
    }
}