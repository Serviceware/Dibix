using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Server.AspNet
{
    internal sealed class HttpResponseMessageFormatter : HttpResponseFormatter<HttpRequestMessageDescriptor>
    {
        public override Task<object> Format(object result, HttpRequestMessageDescriptor request, HttpActionDefinition action, CancellationToken cancellationToken) => Task.FromResult(FormatSync(result, request, action));

        private static object FormatSync(object result, HttpRequestMessageDescriptor request, HttpActionDefinition action)
        {
            if (action.FileResponse != null)
            {
                return CreateFileResponse(result, request, action.FileResponse);
            }

            return result;
        }

        private static object CreateFileResponse(object result, HttpRequestMessageDescriptor request, HttpFileResponseDefinition fileResponse)
        {
            if (result == null)
            {
                return request.RequestMessage.CreateResponse(HttpStatusCode.NotFound);
            }

            string mediaType;
            string fileName;
            HttpContent content;

            switch (result)
            {
                case FileEntity fileEntity:
                    mediaType = MimeTypes.IsRegistered(fileEntity.Type) ? fileEntity.Type : MimeTypes.GetMimeType(fileEntity.Type);
                    fileName = fileEntity.FileName;
                    content = new ByteArrayContent(fileEntity.Data);
                    break;

#if NETFRAMEWORK
                case IJsonFileMetadata jsonFileMetadata:
                    System.Web.Http.HttpConfiguration httpConfiguration = (System.Web.Http.HttpConfiguration)request.RequestMessage.Properties["MS_HttpConfiguration"];
                    mediaType = MimeTypes.GetMimeType("json");
                    fileName = jsonFileMetadata.FileName;
                    content = new ObjectContent(result.GetType(), result, httpConfiguration.Formatters.JsonFormatter);
                    break;
#endif

                default:
                    throw new InvalidOperationException($"Unexpected file result type: {result.GetType()}");
            }

            HttpResponseMessage response = request.RequestMessage.CreateResponse();
            response.Content = content;
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(GetContentDispositionType(fileResponse.DispositionType)) { FileName = fileName };

            if (fileResponse.Cache)
            {
                DateTime now = DateTime.Now;
                TimeSpan year = now.AddYears(1) - now;
                response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = year };
            }

            return response;
        }
    }
}