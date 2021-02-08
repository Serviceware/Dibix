using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Dibix.Http
{
    internal sealed class HeaderParameterSourceProvider : HttpParameterPropertySourceProvider, IHttpParameterSourceProvider
    {
        public const string SourceName = "HEADER";

        protected override Type GetInstanceType(HttpActionDefinition action) => typeof(HttpRequestHeaders);

        protected override Expression GetInstanceValue(Type instanceType, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter) => Expression.Property(requestParameter, nameof(HttpRequestMessage.Headers));
    }
}