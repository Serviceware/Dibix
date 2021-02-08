using System;
using System.Linq.Expressions;

namespace Dibix.Http
{
    public abstract class HttpParameterPropertySourceProvider : IHttpParameterSourceProvider
    {
        public void Resolve(IHttpParameterResolutionContext context)
        {
            Type instanceType = this.GetInstanceType(context.Action);
            Expression instanceValue = this.GetInstanceValue(instanceType, context.RequestParameter, context.ArgumentsParameter, context.DependencyResolverParameter);
            context.ResolveUsingInstanceProperty(instanceType, instanceValue);
        }

        protected abstract Type GetInstanceType(HttpActionDefinition action);

        protected abstract Expression GetInstanceValue(Type instanceType, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter);
    }
}