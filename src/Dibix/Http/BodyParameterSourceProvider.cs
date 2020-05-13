using System;
using System.Linq.Expressions;

namespace Dibix.Http
{
    internal sealed class BodyParameterSourceProvider : IHttpParameterSourceProvider
    {
        public const string SourceName = "BODY";

        public Type GetInstanceType(HttpActionDefinition action) => action.SafeGetBodyContract();

        public Expression GetInstanceValue(Type instanceType, ParameterExpression requestParameter, ParameterExpression argumentsParameter, ParameterExpression dependencyProviderParameter) => Expression.Call(typeof(HttpParameterResolverUtility), nameof(HttpParameterResolverUtility.ReadBody), new [] { instanceType }, argumentsParameter);
    }
}