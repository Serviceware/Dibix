using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Net.Http;

namespace Dibix.Http
{
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal sealed class BodyParameterSourceProvider : IHttpParameterSourceProvider
    {
        public const string SourceName = "BODY";

        public Type GetInstanceType(HttpActionDefinition action) => action.SafeGetBodyContract();

        public Expression GetInstanceValue(Type instanceType, ParameterExpression requestParameter, ParameterExpression argumentsParameter, ParameterExpression dependencyProviderParameter) => Expression.Call(typeof(HttpParameterResolverUtility), nameof(HttpParameterResolverUtility.ReadBody), new [] { instanceType }, argumentsParameter);
    }
}