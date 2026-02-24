using System.Data;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Dibix.Testing;
using Dibix.Testing.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Dibix.Http.Host.Tests
{
    [TestClass]
    public sealed partial class ExceptionHandlingTests : TestBase
    {
        [TestMethod]
        [Endpoint(ActionName = "ThrowClient", Anonymous = true)]
        public async Task InvokeEndpoint_WithClientValidationError_ProducedByThrow_ReturnsProblemDetailsWithCodeAndMessage()
        {
            const string problemDetailsMimeType = "application/problem+json";
            MediaTypeFormatter mediaTypeFormatter = new JsonMediaTypeFormatter { SupportedMediaTypes = { new MediaTypeHeaderValue(problemDetailsMimeType) } };

            TestApplicationFactory app = TestApplicationFactory.Instance;
            HttpClient client = app.CreateClient();

            HttpResponseMessage responseMessage = await client.ExceptionHandlingTests_InvokeEndpoint_WithClientValidationError_ProducedByThrow_ReturnsProblemDetailsWithCodeAndMessage_Endpoint().ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.BadRequest, responseMessage.StatusCode);
            Assert.AreEqual(problemDetailsMimeType, responseMessage.Content.Headers.ContentType?.ToString());
            JObject problemDetails = await responseMessage.Content.ReadAsAsync<JObject>([mediaTypeFormatter]).ConfigureAwait(false);
            AssertJsonResponse(problemDetails);
            Assert.IsFalse(app.Logs.ExceptionHandlerMiddlewareMessages.Any((x => x.Contains($"{TestContext.TestName}_ErrorMessage"))));
        }
        internal static partial void InvokeEndpoint_WithClientValidationError_ProducedByThrow_ReturnsProblemDetailsWithCodeAndMessage_Endpoint(IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using IDatabaseAccessor accessor = databaseAccessorFactory.Create();
            accessor.Execute($"THROW 400001, N'{nameof(InvokeEndpoint_WithClientValidationError_ProducedByThrow_ReturnsProblemDetailsWithCodeAndMessage)}_ErrorMessage', 1", CommandType.Text, ParametersVisitor.Empty);
        }

        [TestMethod]
        [Endpoint(ActionName = "ThrowServer", Anonymous = true, WithAuthorization = true)]
        public async Task InvokeEndpoint_WithServerValidationError_ProducedByThrow_ReturnsProblemDetailsWithoutCodeAndMessage()
        {
            const string problemDetailsMimeType = "application/problem+json";
            MediaTypeFormatter mediaTypeFormatter = new JsonMediaTypeFormatter { SupportedMediaTypes = { new MediaTypeHeaderValue(problemDetailsMimeType) } };

            TestApplicationFactory app = TestApplicationFactory.Instance;
            HttpClient client = app.CreateClient();

            HttpResponseMessage responseMessage = await client.ExceptionHandlingTests_InvokeEndpoint_WithServerValidationError_ProducedByThrow_ReturnsProblemDetailsWithoutCodeAndMessage_Endpoint().ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.InternalServerError, responseMessage.StatusCode);
            Assert.AreEqual(problemDetailsMimeType, responseMessage.Content.Headers.ContentType?.ToString());
            JObject problemDetails = await responseMessage.Content.ReadAsAsync<JObject>([mediaTypeFormatter]).ConfigureAwait(false);
            AssertJsonResponse(problemDetails);
            Assert.IsTrue(app.Logs.ExceptionHandlerMiddlewareMessages.Any((x => x.Contains($"{TestContext.TestName}_ErrorMessage"))));
        }
        internal static partial void InvokeEndpoint_WithServerValidationError_ProducedByThrow_ReturnsProblemDetailsWithoutCodeAndMessage_Endpoint(IDatabaseAccessorFactory databaseAccessorFactory) { }
        internal static partial void InvokeEndpoint_WithServerValidationError_ProducedByThrow_ReturnsProblemDetailsWithoutCodeAndMessage_Authorization(IDatabaseAccessorFactory databaseAccessorFactory)
        {
            using IDatabaseAccessor accessor = databaseAccessorFactory.Create();
            accessor.Execute($"THROW 500001, N'{nameof(InvokeEndpoint_WithServerValidationError_ProducedByThrow_ReturnsProblemDetailsWithoutCodeAndMessage)}_ErrorMessage', 1", CommandType.Text, ParametersVisitor.Empty);
        }

        [TestMethod]
        [Endpoint(ActionName = "ThrowClientWithinClaimsTransformer")]
        public async Task InvokeEndpoint_WithClientValidationError_ProducedByThrow_WithinClaimsTransformer_ReturnsProblemDetailsWithCodeAndMessage()
        {
            const string problemDetailsMimeType = "application/problem+json";
            MediaTypeFormatter mediaTypeFormatter = new JsonMediaTypeFormatter { SupportedMediaTypes = { new MediaTypeHeaderValue(problemDetailsMimeType) } };

            TestApplicationFactory app = TestApplicationFactory.Instance;
            HttpClient client = app.CreateClient();

            JwtSecurityToken jwtToken = new JwtSecurityToken(claims:
            [
                new Claim("SqlErrorNumber", 400001.ToString(CultureInfo.InvariantCulture)),
                new Claim("ValidationErrorMessage", $"{TestContext.TestName}_ErrorMessage")
            ]);
            string bearerToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            HttpResponseMessage responseMessage = await client.ExceptionHandlingTests_InvokeEndpoint_WithClientValidationError_ProducedByThrow_WithinClaimsTransformer_ReturnsProblemDetailsWithCodeAndMessage_Endpoint().ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.BadRequest, responseMessage.StatusCode);
            Assert.AreEqual(problemDetailsMimeType, responseMessage.Content.Headers.ContentType?.ToString());
            JObject problemDetails = await responseMessage.Content.ReadAsAsync<JObject>([mediaTypeFormatter]).ConfigureAwait(false);
            AssertJsonResponse(problemDetails);
            Assert.IsFalse(app.Logs.ExceptionHandlerMiddlewareMessages.Any((x => x.Contains($"{TestContext.TestName}_ErrorMessage"))));
        }
        internal static partial void InvokeEndpoint_WithClientValidationError_ProducedByThrow_WithinClaimsTransformer_ReturnsProblemDetailsWithCodeAndMessage_Endpoint(IDatabaseAccessorFactory databaseAccessorFactory)
        {
        }

        [TestMethod]
        [Endpoint(ActionName = "ThrowServerWithinClaimsTransformer")]
        public async Task InvokeEndpoint_WithServerValidationError_ProducedByThrow_WithinClaimsTransformer_ReturnsProblemDetailsWithCodeAndMessage()
        {
            const string problemDetailsMimeType = "application/problem+json";
            MediaTypeFormatter mediaTypeFormatter = new JsonMediaTypeFormatter { SupportedMediaTypes = { new MediaTypeHeaderValue(problemDetailsMimeType) } };

            TestApplicationFactory app = TestApplicationFactory.Instance;
            HttpClient client = app.CreateClient();

            JwtSecurityToken jwtToken = new JwtSecurityToken(claims:
            [
                new Claim("SqlErrorNumber", 500001.ToString(CultureInfo.InvariantCulture)),
                new Claim("ValidationErrorMessage", $"{TestContext.TestName}_ErrorMessage")
            ]);
            string bearerToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            HttpResponseMessage responseMessage = await client.ExceptionHandlingTests_InvokeEndpoint_WithServerValidationError_ProducedByThrow_WithinClaimsTransformer_ReturnsProblemDetailsWithCodeAndMessage_Endpoint().ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.InternalServerError, responseMessage.StatusCode);
            Assert.AreEqual(problemDetailsMimeType, responseMessage.Content.Headers.ContentType?.ToString());
            JObject problemDetails = await responseMessage.Content.ReadAsAsync<JObject>([mediaTypeFormatter]).ConfigureAwait(false);
            AssertJsonResponse(problemDetails);
            Assert.IsTrue(app.Logs.ExceptionHandlerMiddlewareMessages.Any((x => x.Contains($"{TestContext.TestName}_ErrorMessage"))));
        }
        internal static partial void InvokeEndpoint_WithServerValidationError_ProducedByThrow_WithinClaimsTransformer_ReturnsProblemDetailsWithCodeAndMessage_Endpoint(IDatabaseAccessorFactory databaseAccessorFactory)
        {
        }
    }
}