using System;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Dibix.Http.Server
{
    public sealed class ClaimParameterSourceProvider : NonUserParameterSourceProvider, IHttpParameterSourceProvider
    {
        public static readonly string SourceName = ClaimParameterSource.SourceName;

        public override void Resolve(IHttpParameterResolutionContext context)
        {
            Expression call = Expression.Call(typeof(ClaimParameterSourceProvider), nameof(GetClaimValue), Type.EmptyTypes, context.RequestParameter, Expression.Constant(context.PropertyPath));
            context.ResolveUsingValue(call);
        }

        private static string GetClaimValue(IHttpRequestDescriptor request, string name)
        {
            ClaimsPrincipal principal = request.GetUser();
            Claim claim = principal.Claims.FirstOrDefault(x => x.Type == name);
            return claim?.Value;
        }
    }
}