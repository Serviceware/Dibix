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

            string entityTag = GetEntityTag(result);
            if (entityTag != null)
            {
                // RFC 9110 section 15.4.5: Repeat the validator and cache headers of the 200 response, but no body
                HttpResponseMessage response = result is HttpNotModifiedResult ? request.RequestMessage.CreateResponse(HttpStatusCode.NotModified) : request.RequestMessage.CreateResponse(HttpStatusCode.OK, result);
                AppendEntityTag(response, entityTag);
                return response;
            }

            return result;
        }

        private static void AppendEntityTag(HttpResponseMessage response, string entityTag)
        {
            response.Headers.ETag = new EntityTagHeaderValue(entityTag);
            response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            response.Headers.Vary.Add(ETagVary);
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
                {
                    mediaType = MimeTypes.IsRegistered(fileEntity.Type) ? fileEntity.Type : MimeTypes.GetMimeType(fileEntity.Type);
                    fileName = fileEntity.FileName;
                    content = new StreamContent(fileEntity.Data);

                    if (fileEntity.Length != null)
                        content.Headers.ContentLength = fileEntity.Length;

                    break;
                }

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