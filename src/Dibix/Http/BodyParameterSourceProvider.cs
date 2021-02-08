using System;
using System.Linq.Expressions;

namespace Dibix.Http
{
    internal sealed class BodyParameterSourceProvider : HttpParameterPropertySourceProvider, IHttpParameterSourceProvider
    {
        public const string SourceName = "BODY";

        protected override Type GetInstanceType(HttpActionDefinition action) => action.SafeGetBodyContract();

        protected override Expression GetInstanceValue(Type instanceType, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter) => Expression.Call(typeof(HttpParameterResolverUtility), nameof(HttpParameterResolverUtility.ReadBody), new [] { instanceType }, argumentsParameter);
    }
}