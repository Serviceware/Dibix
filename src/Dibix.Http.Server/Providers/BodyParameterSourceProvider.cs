using System;
using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    internal sealed class BodyParameterSourceProvider : NonUserParameterSourceProvider, IHttpParameterSourceProvider
    {
        public static readonly string SourceName = BodyParameterSource.SourceName;

        public override void Resolve(IHttpParameterResolutionContext context)
        {
            switch (context.PropertyPath)
            {
                case BodyParameterSource.RawPropertyName:
                {
                    Expression getBodyCall = Expression.Call(context.RequestParameter, nameof(IHttpRequestDescriptor.GetBody), Type.EmptyTypes);
                    context.ResolveUsingValue(getBodyCall);
                    break;
                }

                case BodyParameterSource.MediaTypePropertyName:
                {
                    Expression getBodyMediaTypeCall = Expression.Call(context.RequestParameter, nameof(IHttpRequestDescriptor.GetBodyMediaType), Type.EmptyTypes);
                    context.ResolveUsingValue(getBodyMediaTypeCall);
                    break;
                }

                case BodyParameterSource.FileNamePropertyName:
                {
                    Expression getBodyFileNameCall = Expression.Call(context.RequestParameter, nameof(IHttpRequestDescriptor.GetBodyFileName), Type.EmptyTypes);
                    context.ResolveUsingValue(getBodyFileNameCall);
                    break;
                }

                case BodyParameterSource.LengthPropertyName:
                {
                    Expression getBodyLengthCall = Expression.Call(context.RequestParameter, nameof(IHttpRequestDescriptor.GetBodyLength), Type.EmptyTypes);
                    context.ResolveUsingValue(getBodyLengthCall);
                    break;
                }

                default:
                {
                    Type instanceType = context.ActionMetadata.SafeGetBodyContract();
                    Expression instanceValue = Expression.Call(typeof(HttpRuntimeExpressionSupport), nameof(HttpRuntimeExpressionSupport.ReadBody), [instanceType], context.ArgumentsParameter);
                    context.ResolveUsingInstanceProperty(instanceType, instanceValue, ensureNullPropagation: true);
                    break;
                }
            }
        }
    }
}