using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Dibix.Http.Client.Tests
{
    [TestClass]
    public partial class HttpMessageHandlerTests
    {
        [TestMethod]
        [DataRow(true, false, DisplayName = "IWebProxy.IsBypassed = False")]
        [DataRow(true, true, DisplayName = "IWebProxy.IsBypassed = True")]
        [DataRow(false, false, DisplayName = "UseHandler = False")]
        public async Task TraceProxyHandler(bool useHandler, bool isByPassed)
        {
            (HttpClientHandler _, Func<Task> sendInvoker) = SetupFixture<TraceProxyHttpMessageHandler>(useHandler);

            Mock<TraceListener> traceListener = new Mock<TraceListener>(MockBehavior.Strict);

            traceListener.Setup(x => x.TraceEvent(It.IsAny<TraceEventCache>(), "System.Net", TraceEventType.Information, 0, $"[Proxy] IsBypassed http://localhost/: {isByPassed}", null))
                         .Verifiable(useHandler ? Times.Once : Times.Never);

            TraceSource traceSource = GetProxyTraceSource();
            traceSource.Listeners.Add(traceListener.Object);

            try
            {
                Mock<IWebProxy> proxy = new Mock<IWebProxy>(MockBehavior.Strict);

                proxy.Setup(x => x.IsBypassed(new Uri("http://localhost"))).Returns(isByPassed);

                WebRequest.DefaultWebProxy = proxy.Object;

                await sendInvoker().ConfigureAwait(false);
                traceListener.VerifyAll();
            }
            finally
            {
                traceSource.Listeners.Remove(traceListener.Object);
            }
        }

        [TestMethod]
        [DataRow(false, false, HttpStatusCode.NotFound, false, DisplayName = "UseHandler = False")]
        [DataRow(true,  false, HttpStatusCode.NotFound, true,  DisplayName = "HttpStatusCode.NotFound")]
        [DataRow(true,  false, HttpStatusCode.Created,  false, DisplayName = "HttpStatusCode.Created")]
        [DataRow(true,  false, HttpStatusCode.Found,    true,  DisplayName = "HttpStatusCode.Found with AllowAutoRedirect = True")]
        [DataRow(true,  true,  HttpStatusCode.Found,    false, DisplayName = "HttpStatusCode.Found with AllowAutoRedirect = False")]
        public async Task EnsureSuccessStatusCodeHandler(bool useHandler, bool disableAutoRedirect, HttpStatusCode statusCode, bool expectException)
        {
            (HttpClientHandler _, Func<Task> sendInvoker) = SetupFixture<EnsureSuccessStatusCodeHttpMessageHandler>(useHandler, statusCode: statusCode, disableAutoRedirect: disableAutoRedirect);
            try
            {
                await sendInvoker().ConfigureAwait(false);
            }
            catch (HttpException httpException) when(expectException)
            {
                Assert.AreEqual(statusCode, httpException.StatusCode, "httpException.StatusCode");
                return;
            }

            if (expectException)
                Assert.Fail($"Expected {nameof(HttpException)}, but none was thrown");
        }

      //[TestMethod]
      //[Ignore("The TimeoutHttpMessageHandler is not stable enough and currently requires updating the Timeout on the HttpClient which is not ideal")]
      //[DataRow(false, "System.Threading.Tasks.TaskCanceledException: The request was canceled due to the configured HttpClient.Timeout of 0.001 seconds elapsing.", DisplayName = "UseHandler = False")]
      //[DataRow(true, "System.TimeoutException: Timeout for HTTP request has been reached: 00:00:00.0010000", DisplayName = "UseHandler = True")]
      //public async Task TimeoutHandler(bool useHandler, string expectedException)
      //{
      //    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
      //    TimeSpan timeout = TimeSpan.FromMilliseconds(1000d);
      //
      //    TimeoutHttpMessageHandler CreateHandler()
      //    {
      //        Mock<DelegatingHandler> innerHandler = new Mock<DelegatingHandler>(MockBehavior.Strict);
      //
      //        innerHandler.Protected()
      //                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      //                    .Returns(async (HttpRequestMessage requestMessage, CancellationToken _) =>
      //                    {
      //                        // We deliberately not passing the cancellation token to provoke the cancellation within the HttpClient and not within our test
      //                        await Task.Delay(2000).ConfigureAwait(false);
      //
      //                        HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
      //                        {
      //                            RequestMessage = requestMessage,
      //                            Content = new ByteArrayContent(Array.Empty<byte>())
      //                        };
      //                        return responseMessage;
      //                    });
      //
      //        return new TimeoutHttpMessageHandler
      //        {
      //            Timeout = timeout,
      //            InnerHandler = innerHandler.Object
      //        };
      //    }
      //
      //    ;
      //
      //    void ConfigureClient(HttpClient client) => client.Timeout = timeout;
      //
      //    (HttpClientHandler _, Func<Task> sendInvoker) = SetupFixture(useHandler, CreateHandler, configureClient: ConfigureClient);
      //    try
      //    {
      //        await sendInvoker().ConfigureAwait(false);
      //    }
      //    catch (Exception exception)
      //    {
      //        Assert.AreEqual(expectedException, $"{exception.GetType()}: {exception.Message}");
      //        return;
      //    }
      //
      //    Assert.Fail("Expected exception, but none was thrown");
      //}

        [TestMethod]
        [DataRow(false, DisplayName = "UseHandler = False")]
        [DataRow(true, DisplayName = "UseHandler = True")]
        public async Task TracingHandler(bool useHandler)
        {
            Mock<HttpRequestTracer> tracer = new Mock<HttpRequestTracer>(MockBehavior.Strict);

            IProtectedMock<HttpRequestTracer> protectedMock = tracer.Protected();
            protectedMock.Setup<Task>("TraceRequestAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<HttpRequestTrace>()).Returns(Task.CompletedTask);
            protectedMock.Setup<Task>("TraceResponseAsync", ItExpr.IsAny<HttpResponseMessage>(), ItExpr.IsAny<HttpRequestTrace>()).Returns(Task.CompletedTask);

            (HttpClientHandler _, Func<Task> sendInvoker) = SetupFixture(useHandler, () => new TracingHttpMessageHandler(tracer.Object));
            await sendInvoker().ConfigureAwait(false);

            protectedMock.Verify("TraceRequestAsync", useHandler ? Times.Once() : Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<HttpRequestTrace>());
            protectedMock.Verify("TraceResponseAsync", useHandler ? Times.Once() : Times.Never(), ItExpr.IsAny<HttpResponseMessage>(), ItExpr.IsAny<HttpRequestTrace>());
        }
    }
}