using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    internal sealed class HttpResponseMessageFormatter : IHttpResponseFormatter<HttpRequestMessageDescriptor>
    {
        public Task<object> Format(object result, HttpRequestMessageDescriptor request, HttpActionDefinition action) => Task.FromResult(FormatSync(result, request, action));

        private static object FormatSync(object result, HttpRequestMessageDescriptor request, HttpActionDefinition action)
        {
            if (action.FileResponse != null)
            {
                return CreateFileResponse(result, request, action);
            }

            if (result is HttpResponse response)
            {
                return response.CreateResponse(request.RequestMessage);
            }

            return result;
        }

        private static object CreateFileResponse(object result, HttpRequestMessageDescriptor request, HttpActionDefinition action)
        {
            FileEntity file = (FileEntity)result;
            if (file == null)
                return request.RequestMessage.CreateResponse(HttpStatusCode.NotFound);

            string mediaType = MimeTypes.IsRegistered(file.Type) ? file.Type : MimeTypes.GetMimeType(file.Type);

            HttpResponseMessage response = request.RequestMessage.CreateResponse();
            response.Content = new ByteArrayContent(file.Data);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline") { FileName = file.FileName };

            if (action.FileResponse.Cache)
            {
                DateTime now = DateTime.Now;
                TimeSpan year = now.AddYears(1) - now;
                response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = year };
            }

            return response;
        }
    }
}