using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing.Http
{
    internal sealed class TestHttpClientFactoryBuilder : HttpClientFactoryBuilder
    {
        private readonly TestContext _testContext;
        private readonly TextWriter _logger;

        public const string HttpClientName = "Dibix.Testing.TestHttpClient";
        protected override string ClientName => HttpClientName;

        private TestHttpClientFactoryBuilder(TestContext testContext, TextWriter logger)
        {
            _testContext = testContext;
            _logger = logger;
        }

        public static TestHttpClientFactoryBuilder Create(TestContext testContext, TextWriter logger) => new TestHttpClientFactoryBuilder(testContext, logger);

        protected override void Configure(Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder)
        {
            builder.AddHttpMessageHandler(() => new LoggingHttpMessageHandler(_logger));
        }

        protected override void Configure(HttpClient client)
        {
            Assembly testAssembly = TestImplementationResolver.ResolveTestAssembly(_testContext);
            client.AddUserAgent(y => y.FromAssembly(testAssembly));
        }

        private sealed class LoggingHttpMessageHandler : DelegatingHandler
        {
            private readonly TextWriter _output;

            public LoggingHttpMessageHandler(TextWriter output) => _output = output;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await _output.WriteAsync($"{request.Method} {request.RequestUri}").ConfigureAwait(false);
                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    sw.Stop();
                    await _output.WriteAsync($" {(int)response.StatusCode} {response.StatusCode} {sw.Elapsed}").ConfigureAwait(false);
                    return response;
                }
                finally
                {
                    await _output.WriteLineAsync().ConfigureAwait(false);
                }
            }
        }
    }
}