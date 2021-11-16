using System.Net.Http;
using System.Net.Http.Formatting;

namespace Dibix.Http.Client
{
    public static class MediaTypeFormattersFactory
    {
        public static MediaTypeFormatterCollection Create(HttpClient client)
        {
            MediaTypeFormatterCollection mediaTypeFormatterCollection = new MediaTypeFormatterCollection();
            JsonMediaTypeFormatter jsonMediaTypeFormatter = mediaTypeFormatterCollection.JsonFormatter;
            jsonMediaTypeFormatter.SerializerSettings.ContractResolver = new HttpClientJsonContractResolver(client.BaseAddress.Host, jsonMediaTypeFormatter);
            return mediaTypeFormatterCollection;
        }
    }
}