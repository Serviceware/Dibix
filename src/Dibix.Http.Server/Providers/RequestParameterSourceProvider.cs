using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;

namespace Dibix.Http.Server
{
    internal sealed class RequestParameterSourceProvider : NonUserParameterSourceProvider, IHttpParameterSourceProvider
    {
        public const string SourceName = "REQUEST";

        public override void Resolve(IHttpParameterResolutionContext context)
        {
            Expression value = BuildExpression(context.PropertyPath, context.RequestParameter);
            context.ResolveUsingValue(value);
        }

        private static Expression BuildExpression(string propertyName, Expression requestParameter)
        {
            switch (propertyName)
            {
                case "Language": return BuildExpression(requestParameter, nameof(GetFirstLanguage));
                case "Languages": return BuildExpression(requestParameter, nameof(GetLanguages));
                default: throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, null);
            }
        }

        private static Expression BuildExpression(Expression requestParameter, string methodName)
        {
            Expression getLanguageCall = Expression.Call(typeof(RequestParameterSourceProvider), methodName, new Type[0], requestParameter);
            return getLanguageCall;
        }

        private static string GetFirstLanguage(HttpRequestMessage request) => GetLanguages(request).FirstOrDefault();
        
        private static IEnumerable<string> GetLanguages(HttpRequestMessage request) => request.Headers.AcceptLanguage.Select(x => x.Value);
    }
}