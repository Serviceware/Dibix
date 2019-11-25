using System;
using System.Linq.Expressions;
using System.Net.Http;

namespace Dibix.Http
{
    public class HttpParameterDependencySourceProvider : IHttpParameterSourceProvider
    {
        private readonly Type _type;

        public HttpParameterDependencySourceProvider(Type type) => this._type = type;

        public Type GetInstanceType(HttpActionDefinition action) => this._type;

        public Expression GetInstanceValue(Type instanceType, ParameterExpression requestParameter, ParameterExpression argumentsParameter, ParameterExpression dependencyProviderParameter) => Expression.Call(dependencyProviderParameter, nameof(IParameterDependencyResolver.Resolve), new[] { instanceType });
    }
}