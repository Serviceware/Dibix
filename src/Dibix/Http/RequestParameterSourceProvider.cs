using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;

namespace Dibix.Http
{
    internal sealed class RequestParameterSourceProvider : IHttpParameterSourceProvider
    {
        public const string SourceName = "REQUEST";

        public Type GetInstanceType(HttpActionDefinition action) => typeof(RequestParameterSource);

        public Expression GetInstanceValue(Type instanceType, ParameterExpression requestParameter, ParameterExpression argumentsParameter, ParameterExpression dependencyProviderParameter)
        {
            ConstructorInfo constructor = typeof(RequestParameterSource).GetConstructor(new[] { typeof(HttpRequestMessage) });
            return Expression.New(constructor, requestParameter);
        }
    }

    public sealed class RequestParameterSource
    {
        public string Language { get; }

        public RequestParameterSource(HttpRequestMessage request)
        {
            this.Language = request.Headers.AcceptLanguage.Select(x => x.Value).FirstOrDefault() ?? new CultureInfo("en").Name;
        }
    }
}