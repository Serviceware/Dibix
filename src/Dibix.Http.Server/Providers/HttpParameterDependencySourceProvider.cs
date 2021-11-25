using System;
using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    public class HttpParameterDependencySourceProvider : HttpParameterPropertySourceProvider, IHttpParameterSourceProvider
    {
        private readonly Type _type;

        protected internal HttpParameterDependencySourceProvider(Type type) => this._type = type;

        protected override Type GetInstanceType(IHttpParameterResolutionContext context) => this._type;

        protected override Expression GetInstanceValue(Type instanceType, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter) => Expression.Call(dependencyResolverParameter, nameof(IParameterDependencyResolver.Resolve), new[] { instanceType });
    }
}