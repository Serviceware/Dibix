using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Dibix.Http.Server
{
    internal sealed class HeaderParameterSourceProvider : HttpParameterPropertySourceProvider, IHttpParameterSourceProvider
    {
        public const string SourceName = "HEADER";

        protected override Type GetInstanceType(IHttpParameterResolutionContext context) => typeof(HttpRequestHeaders);

        protected override Expression GetInstanceValue(Type instanceType, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter) => Expression.Property(requestParameter, nameof(HttpRequestMessage.Headers));

        protected override string NormalizePropertyPath(string propertyPath)
        {
            string normalizedPropertyPath = Regex.Replace(propertyPath, "[-]", String.Empty);
            return normalizedPropertyPath;
        }
    }
}