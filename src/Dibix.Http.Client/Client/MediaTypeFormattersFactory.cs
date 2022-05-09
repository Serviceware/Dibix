using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using Newtonsoft.Json;

namespace Dibix.Http.Client
{
    public static class MediaTypeFormattersFactory
    {
        public static MediaTypeFormatterCollection Create(HttpClient client)
        {
            Guard.IsNotNull(client, nameof(client));

            if (client.BaseAddress == null)
                throw new InvalidOperationException("client.BaseAddress is null");

            MediaTypeFormatterCollection mediaTypeFormatterCollection = new MediaTypeFormatterCollection();
            JsonMediaTypeFormatter jsonMediaTypeFormatter = mediaTypeFormatterCollection.JsonFormatter;
            jsonMediaTypeFormatter.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
            jsonMediaTypeFormatter.SerializerSettings.ContractResolver = new HttpClientJsonContractResolver(client.BaseAddress.Host, jsonMediaTypeFormatter);
            return mediaTypeFormatterCollection;
        }
    }
}