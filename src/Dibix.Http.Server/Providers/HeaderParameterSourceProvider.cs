using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;

namespace Dibix.Http.Server
{
    internal sealed class HeaderParameterSourceProvider : NonUserParameterSourceProvider, IHttpParameterSourceProvider
    {
        public const string SourceName = "HEADER";

        public override HttpParameterLocation Location => HttpParameterLocation.Header;
        
        public override void Resolve(IHttpParameterResolutionContext context)
        {
            Expression keyParameter = Expression.Constant(context.PropertyPath);
            Expression getHeaderCall = Expression.Call(typeof(HeaderParameterSourceProvider), nameof(GetHeader), new Type[0], context.RequestParameter, keyParameter);
            context.ResolveUsingValue(getHeaderCall);
        }

        private static string GetHeader(HttpRequestMessage request, string key) => request.Headers.TryGetValues(key, out IEnumerable<string> values) ? values.First() : null;
    }
}