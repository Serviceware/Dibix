using System;
using System.Net.Http;
using System.Net.Http.Formatting;

namespace Dibix.Http.Client
{
    public static class MediaTypeFormattersFactory
    {
        public static MediaTypeFormatterCollection Create(HttpClientOptions options, HttpClient client)
        {
            Guard.IsNotNull(client, nameof(client));

            if (client.BaseAddress == null)
                throw new InvalidOperationException("client.BaseAddress is null");

            MediaTypeFormatterCollection mediaTypeFormatterCollection = new MediaTypeFormatterCollection();
            JsonMediaTypeFormatter jsonMediaTypeFormatter = mediaTypeFormatterCollection.JsonFormatter;
            jsonMediaTypeFormatter.SerializerSettings.DateTimeZoneHandling = options.ResponseContent.DateTimeZoneHandling;
            jsonMediaTypeFormatter.SerializerSettings.ContractResolver = new HttpClientJsonContractResolver(client.BaseAddress.Host, jsonMediaTypeFormatter, options.ResponseContent.MakeRelativeUrisAbsolute);
            return mediaTypeFormatterCollection;
        }
    }
}