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
    public partial class HttpClientFactoryTests
    {
        private static (HttpClientHandler httpClientHandler, Func<Task> sendInvoker) SetupFixture(string httpMethod = "GET", HttpStatusCode statusCode = HttpStatusCode.OK, Action<IHttpClientBuilder>? configure = null, Func<CancellationToken, Task>? beforeSend = null)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost");
            HttpResponseMessage responseMessage = new HttpResponseMessage(statusCode) { RequestMessage = requestMessage };

            Mock<HttpClientHandler> httpClientHandler = new Mock<HttpClientHandler>(MockBehavior.Strict);

            httpClientHandler.Protected()
                             .Setup<Task<HttpResponseMessage>>("SendAsync", requestMessage, ItExpr.IsAny<CancellationToken>())
                             .Returns(async (HttpRequestMessage _, CancellationToken cancellationToken) =>
                             {
                                 if (beforeSend != null)
                                     await beforeSend(cancellationToken).ConfigureAwait(false);

                                 return responseMessage;
                             });

            IHttpClientFactory httpClientFactory = new DefaultHttpClientFactory(x =>
            {
                x.ConfigurePrimaryHttpMessageHandler(() => httpClientHandler.Object);
                configure?.Invoke(x);
            });
            HttpClient client = httpClientFactory.CreateClient();

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