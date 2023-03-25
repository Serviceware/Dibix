using System;
using System.Linq;
using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    internal sealed class HeaderParameterSourceProvider : NonUserParameterSourceProvider, IHttpParameterSourceProvider
    {
        public static readonly string SourceName = HeaderParameterSource.SourceName;

        public override HttpParameterLocation Location => HttpParameterLocation.Header;
        
        public override void Resolve(IHttpParameterResolutionContext context)
        {
            Expression keyParameter = Expression.Constant(context.PropertyPath);
            Expression getHeaderCall = Expression.Call(typeof(HeaderParameterSourceProvider), nameof(GetHeader), Type.EmptyTypes, context.RequestParameter, keyParameter);
            context.ResolveUsingValue(getHeaderCall);
        }

        private static string GetHeader(IHttpRequestDescriptor request, string key) => request.GetHeaderValues(key).FirstOrDefault();
    }
}