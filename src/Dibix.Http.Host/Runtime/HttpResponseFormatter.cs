using System;
using System.IO;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;
using HttpResponse = Microsoft.AspNetCore.Http.HttpResponse;

namespace Dibix.Http.Host
{
    internal sealed class HttpResponseFormatter : IHttpResponseFormatter<HttpRequestDescriptor>
    {
        private readonly HttpResponse _response;

        public HttpResponseFormatter(HttpResponse response)
        {
            _response = response;
        }

        public async Task<object?> Format(object result, HttpRequestDescriptor request, HttpActionDefinition action)
        {
            if (action.FileResponse != null)
            {
                await WriteFileResponse(result, action).ConfigureAwait(false);
            }
            else
            {
                await WriteJsonResponse(result).ConfigureAwait(false);
            }

            return null;
        }

        private async Task WriteFileResponse(object result, HttpActionDefinition action)
        {
            FileEntity? file = (FileEntity?)result;
            if (file == null)
            {
                _response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            string mediaType = MimeTypes.IsRegistered(file.Type) ? file.Type : MimeTypes.GetMimeType(file.Type);

            ResponseHeaders responseHeaders = _response.GetTypedHeaders();
            responseHeaders.ContentType = new MediaTypeHeaderValue(mediaType);
            responseHeaders.ContentDisposition = new ContentDispositionHeaderValue("inline") { FileName = file.FileName };
            
            if (action.FileResponse.Cache)
            {
                DateTime now = DateTime.Now;
                TimeSpan year = now.AddYears(1) - now;
                responseHeaders.CacheControl = new CacheControlHeaderValue { MaxAge = year };
            }

            using (MemoryStream memoryStream = new MemoryStream(file.Data))
            {
                await memoryStream.CopyToAsync(_response.Body).ConfigureAwait(false);
            }
        }

        private async Task WriteJsonResponse(object result)
        {
            await _response.WriteAsJsonAsync(result).ConfigureAwait(false);
        }
    }
}