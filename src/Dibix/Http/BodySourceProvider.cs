using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Dibix.Http
{
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal sealed class BodySourceProvider : IHttpParameterSourceProvider
    {
        public Type GetInstanceType(HttpActionDefinition action) => action.SafeGetBodyContract();

        public Expression GetInstanceValue(Type instanceType, ParameterExpression argumentsParameter, ParameterExpression dependencyProviderParameter) => Expression.Call(typeof(HttpParameterResolverUtility), nameof(HttpParameterResolverUtility.ReadBody), new [] { instanceType }, argumentsParameter);
    }
}