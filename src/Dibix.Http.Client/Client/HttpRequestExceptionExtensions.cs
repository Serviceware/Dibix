using Newtonsoft.Json;

namespace Dibix.Http.Client
{
    public static class HttpRequestExceptionExtensions
    {
        public static T ReadContentAsJson<T>(this HttpRequestException exception)
        {
            Guard.IsNotNull(exception, nameof(exception));
            return JsonConvert.DeserializeObject<T>(exception.ResponseContentText);
        }
    }
}
