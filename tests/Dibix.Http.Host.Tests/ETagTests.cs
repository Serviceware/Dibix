using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dibix.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Http.Host.Tests
{
    [TestClass]
    public sealed partial class ETagTests : TestBase
    {
        private const string ETag = "2af35be0";
        private const string ExpectedJson = """{"id":1,"name":"Name"}""";

        [TestMethod]
        [Endpoint(ActionName = "ColdRequest", Anonymous = true, ReturnsResult = true)]
        public async Task InvokeEndpoint_WithoutIfNoneMatch_ReturnsOkWithETag()
        {
            HttpClient client = TestApplicationFactory.Instance.CreateClient();

            HttpResponseMessage responseMessage = await client.ETagTests_InvokeEndpoint_WithoutIfNoneMatch_ReturnsOkWithETag_Endpoint().ConfigureAwait(false);

            await AssertOkWithETag(responseMessage).ConfigureAwait(false);
        }
        internal static partial object InvokeEndpoint_WithoutIfNoneMatch_ReturnsOkWithETag_Endpoint(IDatabaseAccessorFactory databaseAccessorFactory) => new ETagContract { Id = 1, ETag = ETag, Name = "Name" };

        [TestMethod]
        [Endpoint(ActionName = "StaleRevalidation", Anonymous = true, ReturnsResult = true)]
        public async Task InvokeEndpoint_WithStaleIfNoneMatch_ReturnsOkWithNewETag()
        {
            HttpClient client = TestApplicationFactory.Instance.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-None-Match", "\"9c1d7e42\"");

            // The procedure compares the token and returns the current result, because it does not match
            HttpResponseMessage responseMessage = await client.ETagTests_InvokeEndpoint_WithStaleIfNoneMatch_ReturnsOkWithNewETag_Endpoint().ConfigureAwait(false);

            await AssertOkWithETag(responseMessage).ConfigureAwait(false);
        }
        internal static partial object InvokeEndpoint_WithStaleIfNoneMatch_ReturnsOkWithNewETag_Endpoint(IDatabaseAccessorFactory databaseAccessorFactory) => new ETagContract { Id = 1, ETag = ETag, Name = "Name" };

        [TestMethod]
        [Endpoint(ActionName = "MatchingRevalidation", Anonymous = true)]
        public async Task InvokeEndpoint_WithMatchingIfNoneMatch_ProducedByThrow_ReturnsNotModifiedWithoutBodyAndLogging()
        {
            TestApplicationFactory app = TestApplicationFactory.Instance;
            HttpClient client = app.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-None-Match", $"\"{ETag}\"");

            HttpResponseMessage responseMessage = await client.ETagTests_InvokeEndpoint_WithMatchingIfNoneMatch_ProducedByThrow_ReturnsNotModifiedWithoutBodyAndLogging_Endpoint().ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.NotModified, responseMessage.StatusCode);
            Assert.AreEqual($"\"{ETag}\"", responseMessage.Headers.ETag?.ToString());
            Assert.IsTrue(responseMessage.Headers.CacheControl?.NoCache, "Cache-Control: no-cache");
            Assert.IsTrue(responseMessage.Headers.Vary.Contains("Authorization"), "Vary: Authorization");
            Assert.IsEmpty(await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false));
            Assert.IsFalse(app.Logs.ExceptionHandlerMiddlewareMessages.Any(x => x.Contains(ETag)));
        }
        internal static partial void InvokeEndpoint_WithMatchingIfNoneMatch_ProducedByThrow_ReturnsNotModifiedWithoutBodyAndLogging_Endpoint(IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using IDatabaseAccessor accessor = databaseAccessorFactory.Create();
            accessor.Execute($"THROW 304000, N'{ETag}', 1", CommandType.Text, ParametersVisitor.Empty);
        }

        [TestMethod]
        [Endpoint(ActionName = "WithoutETagSource", Anonymous = true, ReturnsResult = true)]
        public async Task InvokeEndpoint_WithoutETagSource_ReturnsOkWithoutETag()
        {
            HttpClient client = TestApplicationFactory.Instance.CreateClient();

            HttpResponseMessage responseMessage = await client.ETagTests_InvokeEndpoint_WithoutETagSource_ReturnsOkWithoutETag_Endpoint().ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.OK, responseMessage.StatusCode);
            Assert.IsNull(responseMessage.Headers.ETag);
            Assert.IsNull(responseMessage.Headers.CacheControl);
            Assert.IsFalse(responseMessage.Headers.Vary.Contains("Authorization"), "Vary: Authorization");
        }
        internal static partial object InvokeEndpoint_WithoutETagSource_ReturnsOkWithoutETag_Endpoint(IDatabaseAccessorFactory databaseAccessorFactory) => new { Id = 1, Name = "Name" };

        private static async Task AssertOkWithETag(HttpResponseMessage responseMessage)
        {
            Assert.AreEqual(HttpStatusCode.OK, responseMessage.StatusCode);
            Assert.AreEqual($"\"{ETag}\"", responseMessage.Headers.ETag?.ToString());
            Assert.IsTrue(responseMessage.Headers.CacheControl?.NoCache, "Cache-Control: no-cache");
            Assert.IsTrue(responseMessage.Headers.Vary.Contains("Authorization"), "Vary: Authorization");

            // The ETag is transport metadata and therefore not part of the body
            string responseText = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.AreEqual(ExpectedJson, responseText);
        }

        // Mirrors a generated contract marked with '$containsEtag'
        private sealed class ETagContract : IETagProvider
        {
            public int Id { get; set; }
            [JsonIgnore]
            public string ETag { get; set; } = "";
            public string Name { get; set; } = "";
        }
    }
}