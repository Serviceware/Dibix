using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Dibix.Http
{
    internal static class HttpParameterResolverUtility
    {
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Helpline.Server.Application.HttpParameterResolverUtility.CreateException(Dibix.Http.HttpActionDefinition,System.String,System.String)")]
        public static Type SafeGetBodyContract(this HttpActionDefinition action)
        {
            if (action.BodyContract == null)
                throw CreateException(action, "The body property on the action is not specified");

            return action.BodyContract;
        }

        public static TBody ReadBody<TBody>(IDictionary<string, object> arguments)
        {
            if (!arguments.TryGetValue(HttpParameterName.Body, out object body))
                throw new InvalidOperationException("Body is missing from arguments list");

            return (TBody)body;
        }

        public static Exception CreateException(HttpActionDefinition action, string message, string parameterName = null)
        {
            StringBuilder sb = new StringBuilder(message);
            if (parameterName != null)
            {
                sb.AppendLine()
                  .Append("Parameter: ")
                  .Append(parameterName);
            }

            sb.AppendLine()
              .Append("at ")
              .Append(action.Method.ToString().ToUpperInvariant())
              .Append(' ')
              .Append(action.ComputedUri);

            return new InvalidOperationException(sb.ToString());
        }
    }
}