using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    internal static class HttpRuntimeExpressionSupport
    {
        public static Type SafeGetBodyContract(this IHttpActionMetadata action)
        {
            if (action.BodyContract == null)
                throw new InvalidOperationException("The body property on the action is not specified");

            return action.BodyContract;
        }

        public static T ReadArgument<T>(IDictionary<string, object> arguments, string key)
        {
            if (!arguments.TryGetValue(key, out object value))
                throw new InvalidOperationException($"Missing parameter argument: {key}");

            T result = (T)value;
            return result;
        }

        public static TBody ReadBody<TBody>(IDictionary<string, object> arguments) => ReadArgument<TBody>(arguments, SpecialHttpParameterName.Body);

        public static Expression BuildReadableArgumentAccessorExpression(Expression argumentsParameter, string key)
        {
            Expression argumentsKey = Expression.Constant(key);
            Expression call = Expression.Call(typeof(HttpRuntimeExpressionSupport), nameof(ReadArgument), [typeof(object)], argumentsParameter, argumentsKey);
            return call;
        }
    }
}