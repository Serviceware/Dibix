﻿using System.Net;
using System.Net.Http;

namespace Dibix.Http
{
    internal static class HttpResponseMessageExtensions
    {
        public static HttpResponseMessage CreateResponse(this HttpRequestMessage request)
        {
            Guard.IsNotNull(request, nameof(request));
            return new HttpResponseMessage { RequestMessage = request };
        }
        public static HttpResponseMessage CreateResponse(this HttpRequestMessage request, HttpStatusCode statusCode)
        {
            Guard.IsNotNull(request, nameof(request));
            return new HttpResponseMessage
            {
                StatusCode = statusCode,
                RequestMessage = request
            };
        }
    }
}