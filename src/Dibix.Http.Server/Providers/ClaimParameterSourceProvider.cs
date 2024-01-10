using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Dibix.Http.Server
{
    public sealed class ClaimParameterSourceProvider : NonUserParameterSourceProvider, IHttpParameterSourceProvider
    {
        private static readonly ICollection<string> MultipleClaimNames = new[]
        {
            "Audiences"
        };
        public static readonly string SourceName = ClaimParameterSource.SourceName;

        public override void Resolve(IHttpParameterResolutionContext context)
        {
            string methodName = MultipleClaimNames.Contains(context.PropertyPath) ? nameof(GetMultipleClaimValue) : nameof(GetSingleClaimValue);
            Expression call = Expression.Call(typeof(ClaimParameterSourceProvider), methodName, Type.EmptyTypes, context.RequestParameter, Expression.Constant(context.PropertyPath));
            context.ResolveUsingValue(call);
        }

        private static string GetSingleClaimValue(IHttpRequestDescriptor request, string name)
        {
            string value = GetClaimValues(request, name).FirstOrDefault();
            return value;
        }

        private static IEnumerable<string> GetMultipleClaimValue(IHttpRequestDescriptor request, string name)
        {
            IEnumerable<string> value = GetClaimValues(request, name);
            return value;
        }

        private static IEnumerable<string> GetClaimValues(IHttpRequestDescriptor request, string name)
        {
            ClaimsPrincipal principal = request.GetUser();
            string claimType = ClaimParameterSource.GetBuiltInClaimTypeOrDefault(name);
            IEnumerable<string> value = principal.Claims
                                                 .Where(x => x.Type == claimType)
                                                 .Select(x => x.Value);
            return value;
        }
    }
}