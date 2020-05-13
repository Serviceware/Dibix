using System;
using System.Collections.Generic;

namespace Dibix.Http
{
    internal static class HttpParameterResolverUtility
    {
        public static Type SafeGetBodyContract(this HttpActionDefinition action)
        {
            if (action.BodyContract == null)
                throw new InvalidOperationException("The body property on the action is not specified");

            return action.BodyContract;
        }

        public static TBody ReadBody<TBody>(IDictionary<string, object> arguments)
        {
            if (!arguments.TryGetValue(HttpParameterName.Body, out object body))
                throw new InvalidOperationException("Body is missing from arguments list");

            return (TBody)body;
        }
    }
}