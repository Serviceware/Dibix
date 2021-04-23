using Newtonsoft.Json;

namespace Dibix.Http.Client
{
    public static class HttpRequestExceptionExtensions
    {
        public static T ReadContentAsJson<T>(this HttpException exception)
        {
            Guard.IsNotNull(exception, nameof(exception));
            return JsonConvert.DeserializeObject<T>(exception.ResponseContentText);
        }

        public static string Format(this HttpException exception)
        {
            string formattedRequest = HttpMessageFormatter.Format(exception.Request, exception.RequestContentText, maskSensitiveData: true);
            string formattedResponse = HttpMessageFormatter.Format(exception.Response, exception.ResponseContentText);
            return HttpMessageFormatter.Format(formattedRequest, formattedResponse);
        }
    }
}
