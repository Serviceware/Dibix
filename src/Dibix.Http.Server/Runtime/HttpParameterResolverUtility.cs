using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dibix.Http.Server
{
    internal static class HttpParameterResolverUtility
    {
        public static Type SafeGetBodyContract(this IHttpActionMetadata action)
        {
            if (action.BodyContract == null)
                throw new InvalidOperationException("The body property on the action is not specified");

            return action.BodyContract;
        }

        public static TResult ReadArgument<TResult>(IDictionary<string, object> arguments, string key)
        {
            if (!arguments.TryGetValue(key, out object value))
                throw new InvalidOperationException($"Argument '{key}' is not available");

            return (TResult)value;
        }

        public static TBody ReadBody<TBody>(IDictionary<string, object> arguments) => ReadArgument<TBody>(arguments, HttpParameterName.Body);

        public static Expression BuildWritableArgumentAccessorExpression(Expression argumentsParameter, string key)
        {
            Expression argumentsKey = Expression.Constant(key);
            Expression property = Expression.Property(argumentsParameter, "Item", argumentsKey);
            return property;
        }

        public static Expression BuildReadableArgumentAccessorExpression(Expression argumentsParameter, string key)
        {
            Expression argumentsKey = Expression.Constant(key);
            Expression call = Expression.Call(typeof(HttpParameterResolverUtility), nameof(ReadArgument), new[] { typeof(object) }, argumentsParameter, argumentsKey);
            return call;
        }
    }
}