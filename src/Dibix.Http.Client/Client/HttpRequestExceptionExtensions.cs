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

        public static string GetFormattedRequestText(this HttpException exception)
        {
            // At this point the HttpContent is already disposed
            // Therefore we have to use the overload that accepts the previously captured response content text
            return HttpMessageFormatter.Format(exception.Request, exception.RequestContentText, maskSensitiveData: true);
        }

        public static string GetFormattedResponseText(this HttpException exception)
        {
            // At this point the HttpContent is already disposed
            // Therefore we have to use the overload that accepts the previously captured response content text
            return HttpMessageFormatter.Format(exception.Response, exception.ResponseContentText);
        }
    }
}
