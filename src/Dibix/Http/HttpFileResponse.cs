using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Dibix.Http
{
    public class HttpFileResponse : HttpResponse
    {
        public string FileName { get; }
        public string MimeType { get; }
        public byte[] Data { get; }
        public bool Cache { get; }

        public HttpFileResponse(HttpStatusCode statusCode, string mimeType, byte[] data, bool cache) : this(statusCode, null, mimeType, data, cache) { }
        public HttpFileResponse(HttpStatusCode statusCode, string fileName, string mimeType, byte[] data, bool cache) : base(statusCode)
        {
            this.FileName = fileName;
            this.MimeType = mimeType;
            this.Data = data;
            this.Cache = cache;
        }

        public override HttpResponseMessage CreateResponse(HttpRequestMessage request)
        {
            HttpResponseMessage response = request.CreateResponse();
            response.Content = new StreamContent(new MemoryStream(this.Data));
            response.Content.Headers.ContentLength = this.Data.Length;
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(this.MimeType);
            if (!String.IsNullOrEmpty(this.FileName))
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = this.FileName };

            if (this.Cache)
            {
                DateTime now = DateTime.Now;
                TimeSpan year = now.AddYears(1) - now;
                response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = year };
            }

            return response;
        }
    }
}