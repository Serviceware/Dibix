using System.Net.Http;
using System.Threading.Tasks;
using Dibix.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Http.Host.Tests
{
    [TestClass]
    public class ExceptionHandlingTests : TestBase
    {
        [TestMethod]
        public async Task InvokeEndpoint_WithClientValidationError_ProducedByThrow_ReturnsProblemDetailsWithCodeAndMessage()
        {
            WebApplicationFactory<Program> factory = TestApplicationFactory.Instance;
            HttpClient client = factory.CreateClient();
            HttpResponseMessage responseMessage = await client.GetAsync($"api/Tests/{nameof(ExceptionHandlingTests)}").ConfigureAwait(false);
        }
    }
}