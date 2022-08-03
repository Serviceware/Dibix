using Newtonsoft.Json;

namespace Dibix.Http.Client
{
    public static class HttpExceptionExtensions
    {
        public static T ReadContentAsJson<T>(this HttpException exception)
        {
            Guard.IsNotNull(exception, nameof(exception));
            return JsonConvert.DeserializeObject<T>(exception.ResponseContentText);
        }

        public static string GetFormattedText(this HttpException exception, int? maxContentLength = null, bool maskSensitiveData = true)
        {
            string formattedRequest = HttpMessageFormatter.Format(exception.Request, exception.RequestContentText, maskSensitiveData, maxContentLength);
            string formattedResponse = HttpMessageFormatter.Format(exception.Response, exception.ResponseContentText, maskSensitiveData, maxContentLength);
            string text = $@"Request
-------
{formattedRequest}

Response
--------
{formattedResponse}";
            return text;
        }
    }
}