using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing.Http
{
    internal sealed class TestHttpClientConfiguration : HttpClientConfiguration
    {
        private readonly TestContext _testContext;
        private readonly TextWriter _logger;
        private readonly Action<IHttpClientBuilder> _additionalClientConfiguration;

        public const string HttpClientName = "Dibix.Testing.TestHttpClient";
        public override string Name => HttpClientName;

        public TestHttpClientConfiguration(TestContext testContext, TextWriter logger, Action<IHttpClientBuilder> additionalClientConfiguration)
        {
            this._testContext = testContext;
            this._logger = logger;
            this._additionalClientConfiguration = additionalClientConfiguration;
        }

        public override void Configure(IHttpClientBuilder builder)
        {
            this._additionalClientConfiguration(builder);
            builder.ConfigureClient(this.ConfigureClient);
            builder.AddHttpMessageHandler(() => new LoggingHttpMessageHandler(this._logger));
        }

        private void ConfigureClient(HttpClient client)
        {
            Assembly testAssembly = TestImplementationResolver.ResolveTestAssembly(this._testContext);
            client.AddUserAgent(y => y.FromAssembly(testAssembly, productName =>
            {
                string normalizedProductName = productName.Replace(".", null);
                return normalizedProductName;
            }));
        }

        private sealed class LoggingHttpMessageHandler : DelegatingHandler
        {
            private readonly TextWriter _output;

            public LoggingHttpMessageHandler(TextWriter output) => this._output = output;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await this._output.WriteAsync($"{request.Method} {request.RequestUri}").ConfigureAwait(false);
                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    sw.Stop();
                    await this._output.WriteAsync($" {(int)response.StatusCode} {response.StatusCode} {sw.Elapsed}").ConfigureAwait(false);
                    return response;
                }
                finally
                {
                    await this._output.WriteLineAsync().ConfigureAwait(false);
                }
            }
        }
    }
}