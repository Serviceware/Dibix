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
                case "Path": return BuildCallExpression(nameof(GetPath), requestParameter);
                case "Language": return BuildCallExpression(nameof(GetFirstLanguage), requestParameter);
                case "Languages": return BuildCallExpression(nameof(GetLanguages), requestParameter);
                case "RemoteName": return BuildCallExpression(nameof(GetRemoteName), requestParameter);
                case "RemoteAddress": return BuildCallExpression(nameof(GetRemoteAddress), requestParameter);
                case "BearerToken": return BuildCallExpression(nameof(GetBearerToken), requestParameter);
                case "BearerTokenExpiresAt": return BuildCallExpression(nameof(GetBearerTokenExpiresAt), requestParameter);
                default: throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, null);
            }
        }

        private static Expression BuildCallExpression(string methodName, Expression requestParameter)
        {
            Expression call = Expression.Call(typeof(RequestParameterSourceProvider), methodName, Type.EmptyTypes, requestParameter);
            return call;
        }

        private static string GetPath(IHttpRequestDescriptor request) => request.GetPath();
        
        private static string GetFirstLanguage(IHttpRequestDescriptor request) => GetLanguages(request).FirstOrDefault();

        private static IEnumerable<string> GetLanguages(IHttpRequestDescriptor request) => request.GetAcceptLanguageValues();

        private static string GetRemoteName(IHttpRequestDescriptor request) => request.GetRemoteName();

        private static string GetRemoteAddress(IHttpRequestDescriptor request) => request.GetRemoteAddress();

        private static string GetBearerToken(IHttpRequestDescriptor request) => request.GetBearerToken();
        
        private static DateTime? GetBearerTokenExpiresAt(IHttpRequestDescriptor request) => request.GetBearerTokenExpiresAt();
    }
}