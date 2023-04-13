using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Dibix.Http.Client.Tests
{
    [TestClass]
    public partial class HttpClientFactoryTests
    {
        [TestMethod]
        [DataRow(true, "GET", true, DisplayName = "GET")]
        [DataRow(true, "POST", false, DisplayName = "POST")]
        [DataRow(false, "UNKNOWN", false, DisplayName = "FollowRedirectsForGetRequests = False")]
        public async Task FollowRedirectHandler(bool followRedirectsForGetRequests, string httpMethod, bool expectedAllowAutoRedirect)
        {
            Action<IHttpClientBuilder>? configure = !followRedirectsForGetRequests ? x => x.FollowRedirectsForGetRequests = false : null /* Rely on the default */;
            (HttpClientHandler httpClientHandler, Func<Task> sendInvoker) = SetupFixture(httpMethod, configure: configure);

            httpClientHandler.AllowAutoRedirect = false;
            Assert.IsFalse(httpClientHandler.AllowAutoRedirect, "httpClientHandler.Object.AllowAutoRedirect");

            await sendInvoker().ConfigureAwait(false);
            Assert.AreEqual(expectedAllowAutoRedirect, httpClientHandler.AllowAutoRedirect, "httpClientHandler.Object.AllowAutoRedirect");
        }

        [TestMethod]
        [DataRow(true, false, DisplayName = "IWebProxy.IsBypassed = False")]
        [DataRow(true, true, DisplayName = "IWebProxy.IsBypassed = True")]
        [DataRow(false, false, DisplayName = "TraceProxy = False")]
        public async Task TraceProxyHandler(bool traceProxy, bool isByPassed)
        {
            Action<IHttpClientBuilder>? configure = !traceProxy ? x => x.TraceProxy = false : null /* Rely on the default */;
            (HttpClientHandler _, Func<Task> sendInvoker) = SetupFixture(configure: configure);

            Mock<TraceListener> traceListener = new Mock<TraceListener>(MockBehavior.Strict);

            Expression<Action<TraceListener>> expression = x => x.TraceEvent(It.IsAny<TraceEventCache>(), "System.Net", TraceEventType.Information, 0, $"[Proxy] IsBypassed http://localhost/: {isByPassed}", null);
            traceListener.Setup(expression);

            TraceSource traceSource = GetProxyTraceSource();
            traceSource.Listeners.Add(traceListener.Object);

            try
            {
                Mock<IWebProxy> proxy = new Mock<IWebProxy>(MockBehavior.Strict);

                proxy.Setup(x => x.IsBypassed(new Uri("http://localhost"))).Returns(isByPassed);

                WebRequest.DefaultWebProxy = proxy.Object;

                await sendInvoker().ConfigureAwait(false);
                traceListener.Verify(expression, traceProxy ? Times.Once : Times.Never);
            }
            finally
            {
                traceSource.Listeners.Remove(traceListener.Object);
            }
        }

        [TestMethod]
        [DataRow(false, HttpStatusCode.NotFound, false, DisplayName = "EnsureSuccessStatusCode = False")]
        [DataRow(true, HttpStatusCode.NotFound, true, DisplayName = "HttpStatusCode.NotFound")]
        [DataRow(true, HttpStatusCode.Created, false, DisplayName = "HttpStatusCode.Created")]
        public async Task EnsureSuccessStatusCodeHandler(bool ensureSuccessStatusCode, HttpStatusCode statusCode, bool expectException)
        {
            Action<IHttpClientBuilder>? configure = !ensureSuccessStatusCode ? x => x.EnsureSuccessStatusCode = false : null /* Rely on the default */;
            (HttpClientHandler _, Func<Task> sendInvoker) = SetupFixture(statusCode: statusCode, configure: configure);
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

        [TestMethod]
        [DataRow(false, "System.Threading.Tasks.TaskCanceledException: The request was canceled due to the configured HttpClient.Timeout of 0.001 seconds elapsing.", DisplayName = "WrapTimeoutsInException = False")]
        [DataRow(true, "System.TimeoutException: Timeout for HTTP request has been reached: 00:00:00.0010000", DisplayName = "WrapTimeoutsInException = True")]
        public async Task TimeoutHandler(bool wrapTimeoutsInException, string expectedException)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            void Configure(IHttpClientBuilder builder)
            {
                if (!wrapTimeoutsInException) // Rely on the default
                    builder.WrapTimeoutsInException = false;

                builder.ConfigureClient(x => x.Timeout = TimeSpan.FromMilliseconds(1d));
            }

            (HttpClientHandler _, Func<Task> sendInvoker) = SetupFixture(configure: Configure, beforeSend: cancellationToken => Task.Delay(500, cancellationToken));
            try
            {
                await sendInvoker().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Assert.AreEqual(expectedException, $"{exception.GetType()}: {exception.Message}");
                return;
            }

            Assert.Fail("Expected exception, but none was thrown");
        }

        [TestMethod]
        [DataRow(false, DisplayName = "WithoutTracer")]
        [DataRow(true, DisplayName = "WithTracer")]
        public async Task TracingHandler(bool registerTracer)
        {
            Mock<HttpRequestTracer> tracer = new Mock<HttpRequestTracer>(MockBehavior.Strict);

            IProtectedMock<HttpRequestTracer> protectedMock = tracer.Protected();
            protectedMock.Setup<Task>("TraceRequestAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<HttpRequestTrace>()).Returns(Task.CompletedTask);
            protectedMock.Setup<Task>("TraceResponseAsync", ItExpr.IsAny<HttpResponseMessage>(), ItExpr.IsAny<HttpRequestTrace>()).Returns(Task.CompletedTask);

            Action<IHttpClientBuilder>? configure = registerTracer ? x => x.Tracer = tracer.Object : null /* Rely on the default */;
            (HttpClientHandler _, Func<Task> sendInvoker) = SetupFixture(configure: configure);
            await sendInvoker().ConfigureAwait(false);

            protectedMock.Verify("TraceRequestAsync", registerTracer ? Times.Once() : Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<HttpRequestTrace>());
            protectedMock.Verify("TraceResponseAsync", registerTracer ? Times.Once() : Times.Never(), ItExpr.IsAny<HttpResponseMessage>(), ItExpr.IsAny<HttpRequestTrace>());
        }
    }
}