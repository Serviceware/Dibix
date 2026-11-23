using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
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
                AppendEntityTag(GetEntityTag(result));

                // RFC 9110 section 15.4.5: Repeat the validator and cache headers of the 200 response, but no body
                if (result is HttpNotModifiedResult)
                    _response.StatusCode = StatusCodes.Status304NotModified;
                else
                    await WriteJsonResponse(result, writeIndented: false, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        private void AppendEntityTag(string? entityTag)
        {
            if (entityTag == null)
                return;

            ResponseHeaders responseHeaders = _response.GetTypedHeaders();
            responseHeaders.ETag = new EntityTagHeaderValue(entityTag);
            responseHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            _response.Headers.Append(HeaderNames.Vary, ETagVary);
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

            if (fileResponse.Cache)
            {
                responseHeaders.CacheControl = new CacheControlHeaderValue
                {
                    Public = true,
                    MaxAge = TimeSpan.FromDays(365d), // 1 year
                    Extensions = { new NameValueHeaderValue("immutable") }
                };
            }

            switch (result)
            {
                case FileEntity fileEntity:
                {
                    string mediaType = MimeTypes.IsRegistered(fileEntity.Type) ? fileEntity.Type : MimeTypes.GetMimeType(fileEntity.Type);
                    responseHeaders.ContentType = new MediaTypeHeaderValue(mediaType);
                    AppendFileName(responseHeaders, fileEntity.FileName);

                    if (fileEntity.Length != null)
                        responseHeaders.ContentLength = fileEntity.Length;

                    // Taken from Microsoft.AspNetCore.Internal.FileResultHelper.WriteFileAsync
                    try
                    {
                        await StreamCopyOperation.CopyToAsync(fileEntity.Data, _response.Body, count: null, bufferSize: 64 * 1024, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _response.HttpContext.Abort();
                    }
                    break;
                }

                case IJsonFileMetadata jsonFileMetadata:
                    AppendFileName(responseHeaders, jsonFileMetadata.FileName);
                    await WriteJsonResponse(result, fileResponse.IndentJson, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected file result type: {result.GetType()}");
            }
        }

        private async Task WriteJsonResponse(object? result, bool writeIndented, CancellationToken cancellationToken)
        {
            if (result == null)
            {
                _response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            if (writeIndented)
            {
                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
                await _response.WriteAsJsonAsync(result, jsonSerializerOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _response.WriteAsJsonAsync(result, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
    }
}