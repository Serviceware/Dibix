using System;
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
                case "Language": return BuildLanguageExpression(requestParameter);
                default: throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, null);
            }
        }

        private static Expression BuildLanguageExpression(Expression requestParameter)
        {
            Expression getLanguageCall = Expression.Call(typeof(RequestParameterSourceProvider), nameof(GetLanguage), new Type[0], requestParameter);
            return getLanguageCall;
        }

        private static string GetLanguage(HttpRequestMessage request) => request.Headers.AcceptLanguage.Select(x => x.Value).FirstOrDefault();
    }
}