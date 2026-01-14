using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Dibix.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Dibix.Http.Host.Tests
{
    [TestClass]
    public sealed class ExceptionHandlingTests : TestBase
    {
        [TestMethod]
        public async Task InvokeEndpoint_WithClientValidationError_ProducedByThrow_ReturnsProblemDetailsWithCodeAndMessage()
        {
            const string problemDetailsMimeType = "application/problem+json";
            MediaTypeFormatter mediaTypeFormatter = new JsonMediaTypeFormatter { SupportedMediaTypes = { new MediaTypeHeaderValue(problemDetailsMimeType) } };

            TestApplicationFactory app = TestApplicationFactory.Instance;
            HttpClient client = app.CreateClient();

            HttpResponseMessage responseMessage = await client.GetAsync($"api/Tests/{nameof(ExceptionHandlingTests)}/ThrowClient").ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.BadRequest, responseMessage.StatusCode);
            Assert.AreEqual(problemDetailsMimeType, responseMessage.Content.Headers.ContentType?.ToString());
            JObject problemDetails = await responseMessage.Content.ReadAsAsync<JObject>([mediaTypeFormatter]).ConfigureAwait(false);
            AssertJsonResponse(problemDetails);
            Assert.IsFalse(app.Logs.ExceptionHandlerMiddlewareMessages.Any((x => x.Contains($"{TestContext.TestName}_ErrorMessage"))));
        }
        [ActionName("ThrowClient")]
        private static void InvokeEndpoint_WithClientValidationError_ProducedByThrow_ReturnsProblemDetailsWithCodeAndMessage_Endpoint(IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using IDatabaseAccessor accessor = databaseAccessorFactory.Create();
            accessor.Execute($"THROW 400001, N'{nameof(InvokeEndpoint_WithClientValidationError_ProducedByThrow_ReturnsProblemDetailsWithCodeAndMessage)}_ErrorMessage', 1", CommandType.Text, ParametersVisitor.Empty);
        }

        [TestMethod]
        public async Task InvokeEndpoint_WithServerValidationError_ProducedByThrow_ReturnsProblemDetailsWithoutCodeAndMessage()
        {
            const string problemDetailsMimeType = "application/problem+json";
            MediaTypeFormatter mediaTypeFormatter = new JsonMediaTypeFormatter { SupportedMediaTypes = { new MediaTypeHeaderValue(problemDetailsMimeType) } };

            TestApplicationFactory app = TestApplicationFactory.Instance;
            HttpClient client = app.CreateClient();

            HttpResponseMessage responseMessage = await client.GetAsync($"api/Tests/{nameof(ExceptionHandlingTests)}/ThrowServer").ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.InternalServerError, responseMessage.StatusCode);
            Assert.AreEqual(problemDetailsMimeType, responseMessage.Content.Headers.ContentType?.ToString());
            JObject problemDetails = await responseMessage.Content.ReadAsAsync<JObject>([mediaTypeFormatter]).ConfigureAwait(false);
            AssertJsonResponse(problemDetails);
            Assert.IsTrue(app.Logs.ExceptionHandlerMiddlewareMessages.Any((x => x.Contains($"{TestContext.TestName}_ErrorMessage"))));
        }
        [ActionName("ThrowServer")]
        private static void InvokeEndpoint_WithServerValidationError_ProducedByThrow_ReturnsProblemDetailsWithoutCodeAndMessage_Endpoint() { }
        private static void InvokeEndpoint_WithServerValidationError_ProducedByThrow_ReturnsProblemDetailsWithoutCodeAndMessage_Authorization(IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using IDatabaseAccessor accessor = databaseAccessorFactory.Create();
            accessor.Execute($"THROW 500001, N'{nameof(InvokeEndpoint_WithServerValidationError_ProducedByThrow_ReturnsProblemDetailsWithoutCodeAndMessage)}_ErrorMessage', 1", CommandType.Text, ParametersVisitor.Empty);
        }
    }
}