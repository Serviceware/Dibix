using System;
using System.Net.Http;

namespace Dibix.Http.Client
{
    internal static class HttpRequestPropertyExtensions
    {
        private const string PropertyKeyHttpRequestTrace = "Dibix.Http.Client.HttpRequestTrace";

        public static HttpRequestTrace GetHttpRequestTrace(this HttpRequestMessage requestMessage) => GetRequestProperty<HttpRequestTrace>(requestMessage, PropertyKeyHttpRequestTrace);

        public static void SetHttpRequestTrace(this HttpRequestMessage requestMessage, HttpRequestTrace requestTrace) => SetRequestProperty(requestMessage, PropertyKeyHttpRequestTrace, requestTrace);

        private static T GetRequestProperty<T>(HttpRequestMessage requestMessage, string propertyName)
        {
            Guard.IsNotNull(requestMessage, nameof(requestMessage));
            if (!TryGetPropertyValue(requestMessage, propertyName, out T value))
                throw new InvalidOperationException(FormattableString.Invariant($"Missing property '{propertyName}' on HTTP request message"));

            return value;
        }

        private static void SetRequestProperty<T>(HttpRequestMessage requestMessage, string propertyName, T value)
        {
#if NETCOREAPP
            requestMessage.Options.Set(new HttpRequestOptionsKey<T>(propertyName), value);
#else
            requestMessage.Properties[propertyName] = value;
#endif
        }

        private static bool TryGetPropertyValue<T>(HttpRequestMessage requestMessage, string propertyName, out T value)
        {
#if NETCOREAPP
            return requestMessage.Options.TryGetValue(new HttpRequestOptionsKey<T>(propertyName), out value);
#else
            if (requestMessage.Properties.TryGetValue(propertyName, out object rawValue))
            {
                value = (T)rawValue;
                return true;
            }
            value = default;
            return false;
#endif
        }
    }
}