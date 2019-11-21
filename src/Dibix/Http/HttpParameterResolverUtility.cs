using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Dibix.Http
{
    internal static class HttpParameterResolverUtility
    {
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Helpline.Server.Application.HttpParameterResolverUtility.CreateException(Dibix.Http.HttpActionDefinition,System.String,System.String)")]
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