using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;

namespace Dibix.Http
{
    internal sealed class RequestParameterSourceProvider : IHttpParameterSourceProvider
    {
        public const string SourceName = "REQUEST";

        public void Resolve(IHttpParameterResolutionContext context)
        {
            Expression value = BuildExpression(context.PropertyPath, context.RequestParameter);
            context.ResolveUsingValue(value);
        }

        private static Expression BuildExpression(string propertyName, Expression requestParameter)
        {
            switch (propertyName)
            {
                case "Language": return BuildLanguageExpression(requestParameter);
                default: throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, null);
            }
        }

        private static Expression BuildLanguageExpression(Expression requestParameter)
        {
            Expression getRequestLanguageCall = Expression.Call(typeof(RequestParameterSourceProvider), nameof(GetRequestLanguage), new Type[0], requestParameter);
            return getRequestLanguageCall;
        }

        private static string GetRequestLanguage(HttpRequestMessage request) => request.Headers.AcceptLanguage.Select(x => x.Value).FirstOrDefault() ?? new CultureInfo("en").Name;
    }
}