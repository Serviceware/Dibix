using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    internal sealed class RequestParameterSourceProvider : NonUserParameterSourceProvider, IHttpParameterSourceProvider
    {
        public static readonly string SourceName = RequestParameterSource.SourceName;

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
            Expression getLanguageCall = Expression.Call(typeof(RequestParameterSourceProvider), methodName, Type.EmptyTypes, requestParameter);
            return getLanguageCall;
        }

        private static string GetFirstLanguage(IHttpRequestDescriptor request) => GetLanguages(request).FirstOrDefault();
        
        private static IEnumerable<string> GetLanguages(IHttpRequestDescriptor request) => request.GetAcceptLanguageValues();
    }
}