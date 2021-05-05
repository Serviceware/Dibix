using System;
using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    public abstract class HttpParameterPropertySourceProvider : IHttpParameterSourceProvider
    {
        public void Resolve(IHttpParameterResolutionContext context)
        {
            Type instanceType = this.GetInstanceType(context);
            Expression instanceValue = this.GetInstanceValue(instanceType, context.RequestParameter, context.ArgumentsParameter, context.DependencyResolverParameter);
            context.ResolveUsingInstanceProperty(instanceType, instanceValue, ensureNullPropagation: false, this.NormalizePropertyPath(context.PropertyPath));
        }

        protected abstract Type GetInstanceType(IHttpParameterResolutionContext context);

        protected abstract Expression GetInstanceValue(Type instanceType, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter);

        protected virtual string NormalizePropertyPath(string propertyPath) => propertyPath;
    }
}