using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Dibix.Http.Server
{
    internal sealed class HttpFileResponseFormatter : HttpResponseFormatter
    {
        protected override bool CanFormatResult(HttpActionDefinition actionDefinition, object result) => actionDefinition.FileResponse != null;

        protected override object FormatResult(HttpActionDefinition actionDefinition, object result, HttpRequestMessage requestMessage)
        {
            FileEntity file = (FileEntity)result;
            if (file == null)
                return requestMessage.CreateResponse(HttpStatusCode.NotFound);

            string mediaType = MimeTypes.GetMimeType(file.Type);

            HttpResponseMessage response = requestMessage.CreateResponse();
            response.Content = new StreamContent(new MemoryStream(file.Data));
            response.Content.Headers.ContentLength = file.Data.Length;
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            if (!String.IsNullOrEmpty(file.FileName))
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = file.FileName };

            if (actionDefinition.FileResponse.Cache)
            {
                DateTime now = DateTime.Now;
                TimeSpan year = now.AddYears(1) - now;
                response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = year };
            }

            return response;
        }
    }
}