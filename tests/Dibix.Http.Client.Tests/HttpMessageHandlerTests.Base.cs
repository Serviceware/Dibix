using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Dibix.Http.Client.Tests
{
    public partial class HttpMessageHandlerTests
    {
        private static (HttpClientHandler httpClientHandler, Func<Task> sendInvoker) SetupFixture<THandler>(bool useHandler, string httpMethod = "GET", HttpStatusCode statusCode = HttpStatusCode.OK, bool disableAutoRedirect = false, Action<HttpClient>? configureClient = null) where THandler : DelegatingHandler, new()
        {
            THandler CreateHandler() => new THandler();
            return SetupFixture(useHandler, CreateHandler, httpMethod, statusCode, disableAutoRedirect, configureClient);
        }
        private static (HttpClientHandler httpClientHandler, Func<Task> sendInvoker) SetupFixture<THandler>(bool useHandler, Func<THandler> handlerFactory, string httpMethod = "GET", HttpStatusCode statusCode = HttpStatusCode.OK, bool disableAutoRedirect = false, Action<HttpClient>? configureClient = null) where THandler : DelegatingHandler
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost");
            HttpResponseMessage responseMessage = new HttpResponseMessage(statusCode) { RequestMessage = requestMessage, Content = new ByteArrayContent(Array.Empty<byte>()) };

            Mock<HttpClientHandler> httpClientHandler = new Mock<HttpClientHandler>(MockBehavior.Strict);

            httpClientHandler.Protected()
                             .Setup<Task<HttpResponseMessage>>("SendAsync", requestMessage, ItExpr.IsAny<CancellationToken>())
                             .Returns((HttpRequestMessage _, CancellationToken _) => Task.FromResult(responseMessage));

            if (disableAutoRedirect)
                httpClientHandler.Object.AllowAutoRedirect = false;

            HttpMessageHandler handler = httpClientHandler.Object;

            if (useHandler)
            {
                THandler outerHandler = handlerFactory();

                DelegatingHandler innerHandler = outerHandler;
                while (true)
                {
                    if (innerHandler.InnerHandler is DelegatingHandler delegatingHandler)
                        innerHandler = delegatingHandler;
                    else
                        break;
                }

                innerHandler.InnerHandler = handler;
                handler = outerHandler;
            }

            HttpClient client = new HttpClient(handler);
            configureClient?.Invoke(client);

            async Task SendAsync()
            {
                HttpResponseMessage response = await client.SendAsync(requestMessage).ConfigureAwait(false);
                Assert.AreEqual(responseMessage, response, nameof(responseMessage));
            }

            return (httpClientHandler.Object, SendAsync);
        }

        private static TraceSource GetProxyTraceSource()
        {
            const string fieldName = "ProxyTraceSource";

            Type type = typeof(TraceProxyHttpMessageHandler);
            FieldInfo? field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
                throw new InvalidOperationException($"Could not find 'private static {fieldName}' field on type '{type}'");

            TraceSource? traceSource = (TraceSource?)field.GetValue(null);
            if (traceSource == null)
                throw new InvalidOperationException($"'private static {fieldName}' field on type '{type}' is null");

            traceSource.Switch.Level = SourceLevels.Information;
            return traceSource;
        }
    }
}