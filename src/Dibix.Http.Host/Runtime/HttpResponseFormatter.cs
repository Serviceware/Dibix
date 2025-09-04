using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;

namespace Dibix.Http.Host
{
    internal sealed class HttpResponseFormatter : HttpResponseFormatter<HttpRequestDescriptor>
    {
        private readonly HttpResponse _response;

        public HttpResponseFormatter(HttpResponse response)
        {
            _response = response;
        }

        public override async Task<object?> Format(object? result, HttpRequestDescriptor request, HttpActionDefinition action, CancellationToken cancellationToken)
        {
            if (action.FileResponse != null)
            {
                await WriteFileResponse(result, action.FileResponse, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await WriteJsonResponse(result, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        private async Task WriteFileResponse(object? result, HttpFileResponseDefinition fileResponse, CancellationToken cancellationToken)
        {
            void AppendFileName(ResponseHeaders responseHeaders, string fileName)
            {
                responseHeaders.ContentDisposition = new ContentDispositionHeaderValue(GetContentDispositionType(fileResponse.DispositionType)) { FileName = fileName };
            }

            if (result == null)
            {
                _response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            ResponseHeaders responseHeaders = _response.GetTypedHeaders();

            switch (result)
            {
                case FileEntity fileEntity:
                {
                    string mediaType = MimeTypes.IsRegistered(fileEntity.Type) ? fileEntity.Type : MimeTypes.GetMimeType(fileEntity.Type);
                    responseHeaders.ContentType = new MediaTypeHeaderValue(mediaType);
                    AppendFileName(responseHeaders, fileEntity.FileName);

                    using MemoryStream memoryStream = new MemoryStream(fileEntity.Data);
                    await memoryStream.CopyToAsync(_response.Body, cancellationToken).ConfigureAwait(false);
                    break;
                }

                case IJsonFileMetadata jsonFileMetadata:
                    AppendFileName(responseHeaders, jsonFileMetadata.FileName);
                    await WriteJsonResponse(result, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected file result type: {result.GetType()}");
            }
        }

        private async Task WriteJsonResponse(object? result, CancellationToken cancellationToken)
        {
            if (result == null)
            {
                _response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            await _response.WriteAsJsonAsync(result, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}