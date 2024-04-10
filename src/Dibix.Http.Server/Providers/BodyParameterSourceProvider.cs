using System;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    internal sealed class BodyParameterSourceProvider : NonUserParameterSourceProvider, IHttpParameterSourceProvider
    {
        public static readonly string SourceName = BodyParameterSource.SourceName;

        public override void Resolve(IHttpParameterResolutionContext context)
        {
            if (context.PropertyPath != BodyParameterSource.RawPropertyName)
            {
                Type instanceType = context.ActionMetadata.SafeGetBodyContract();
                Expression instanceValue = Expression.Call(typeof(HttpParameterResolverUtility), nameof(HttpParameterResolverUtility.ReadBody), new[] { instanceType }, context.ArgumentsParameter);
                context.ResolveUsingInstanceProperty(instanceType, instanceValue, ensureNullPropagation: true);
            }
            else
            {
                // TODO: Can be null!
                Expression getBodyCall = Expression.Call(context.RequestParameter, nameof(IHttpRequestDescriptor.GetBody), Type.EmptyTypes);
                Expression getAwaiterCall = Expression.Call(getBodyCall, typeof(Task<Stream>).SafeGetMethod(nameof(Task<Stream>.GetAwaiter)));
                Expression getResultCall = Expression.Call(getAwaiterCall, nameof(TaskAwaiter.GetResult), Type.EmptyTypes);
                context.ResolveUsingValue(getResultCall);
            }
        }
    }
}