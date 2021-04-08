using System;
using System.IO;
using System.Linq.Expressions;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Dibix.Http
{
    internal sealed class BodyParameterSourceProvider : IHttpParameterSourceProvider
    {
        public const string SourceName = "BODY";
        public const string RawPropertyName = "$RAW";

        public void Resolve(IHttpParameterResolutionContext context)
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
                Expression readAsStreamAsyncCall = Expression.Call(contentProperty, nameof(HttpContent.ReadAsStreamAsync), new Type[0]);
                Expression getAwaiterCall = Expression.Call(readAsStreamAsyncCall, typeof(Task<Stream>).GetMethod(nameof(Task<Stream>.GetAwaiter)));
                Expression getResultCall = Expression.Call(getAwaiterCall, nameof(TaskAwaiter.GetResult), new Type[0]);
                context.ResolveUsingValue(getResultCall);
            }
        }
    }
}