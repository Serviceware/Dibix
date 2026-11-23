﻿using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dibix.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Http.Host.Tests
{
    [TestClass]
    public sealed partial class CompressionTests : TestBase
    {
        private static readonly string ResponseText = new string('x', 1024);
        private static readonly string ExpectedJson = $$"""{"text":"{{ResponseText}}"}""";

        [TestMethod]
        [Endpoint(ActionName = "Compressed", Anonymous = true, Compress = true, ReturnsResult = true)]
        public async Task InvokeEndpoint_WithCompress_ReturnsCompressedResponse()
        {
            HttpClient client = CreateClient();

            HttpResponseMessage responseMessage = await client.CompressionTests_InvokeEndpoint_WithCompress_ReturnsCompressedResponse_Endpoint().ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.OK, responseMessage.StatusCode);
            Assert.AreEqual("gzip", String.Join(",", responseMessage.Content.Headers.ContentEncoding));
            Assert.IsTrue(responseMessage.Headers.Vary.Contains("Accept-Encoding"), "Vary: Accept-Encoding");

            await using Stream responseStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
            await using GZipStream decompressionStream = new GZipStream(responseStream, CompressionMode.Decompress);
            using StreamReader reader = new StreamReader(decompressionStream);
            string responseText = await reader.ReadToEndAsync().ConfigureAwait(false);
            Assert.AreEqual(ExpectedJson, responseText);
        }
        internal static partial object InvokeEndpoint_WithCompress_ReturnsCompressedResponse_Endpoint(IDatabaseAccessorFactory databaseAccessorFactory) => new { Text = ResponseText };

        [TestMethod]
        [Endpoint(ActionName = "Uncompressed", Anonymous = true, ReturnsResult = true)]
        public async Task InvokeEndpoint_WithoutCompress_ReturnsUncompressedResponse()
        {
            HttpClient client = CreateClient();

            HttpResponseMessage responseMessage = await client.CompressionTests_InvokeEndpoint_WithoutCompress_ReturnsUncompressedResponse_Endpoint().ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.OK, responseMessage.StatusCode);
            Assert.AreEqual(0, responseMessage.Content.Headers.ContentEncoding.Count);

            string responseText = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.AreEqual(ExpectedJson, responseText);
        }
        internal static partial object InvokeEndpoint_WithoutCompress_ReturnsUncompressedResponse_Endpoint(IDatabaseAccessorFactory databaseAccessorFactory) => new { Text = ResponseText };

        private static HttpClient CreateClient()
        {
            HttpClient client = TestApplicationFactory.Instance.CreateClient();
            client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip");
            return client;
        }
    }
}