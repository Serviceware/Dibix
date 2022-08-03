using System.Net.Http;

namespace Dibix.Http.Client
{
    public static class HttpRequestMessageExtensions
    {
        public static string GetFormattedText(this HttpRequestMessage requestMessage, string requestContentText, int? maxContentLength = null, bool maskSensitiveData = true)
        {
            return HttpMessageFormatter.Format(requestMessage, requestContentText, maskSensitiveData, maxContentLength);
        }

        public static string GetFormattedText(this HttpResponseMessage responseMessage, string responseContentText, int? maxContentLength = null, bool maskSensitiveData = true)
        {
            return HttpMessageFormatter.Format(responseMessage, responseContentText, maskSensitiveData, maxContentLength);
        }
    }
}