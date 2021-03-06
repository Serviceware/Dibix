using System;
using System.IO;
using System.Linq.Expressions;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    internal sealed class BodyParameterSourceProvider : NonUserParameterSourceProvider, IHttpParameterSourceProvider
    {
        public static readonly string SourceName = BodyParameterSource.SourceName;
        public const string RawPropertyName = "$RAW";

        public override void Resolve(IHttpParameterResolutionContext context)
        {
            if (context.PropertyPath != RawPropertyName)
            {
                Type instanceType = context.Action.SafeGetBodyContract();
                Expression instanceValue = Expression.Call(typeof(HttpParameterResolverUtility), nameof(HttpParameterResolverUtility.ReadBody), new[] { instanceType }, context.ArgumentsParameter);
                context.ResolveUsingInstanceProperty(instanceType, instanceValue, ensureNullPropagation: true);
            }
            else
            {
                Expression contentProperty = Expression.Property(context.RequestParameter, nameof(HttpRequestMessage.Content));
                Expression readAsStreamAsyncCall = Expression.Call(contentProperty, nameof(HttpContent.ReadAsStreamAsync), Type.EmptyTypes);
                Expression getAwaiterCall = Expression.Call(readAsStreamAsyncCall, typeof(Task<Stream>).GetMethod(nameof(Task<Stream>.GetAwaiter)));
                Expression getResultCall = Expression.Call(getAwaiterCall, nameof(TaskAwaiter.GetResult), Type.EmptyTypes);
                context.ResolveUsingValue(getResultCall);
            }
        }
    }
}