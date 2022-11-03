using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Http.Server.Tests
{
    [TestClass]
    public partial class HttpActionInvokerTest
    {
        [TestMethod]
        public async Task Invoke_WithResult()
        {
            object result = await Execute().ConfigureAwait(false);
            Assert.AreEqual(1, result);
        }
        private static int Invoke_WithResult_Target(IDatabaseAccessorFactory databaseAccessorFactory) => 1;

        [TestMethod]
        public async Task Invoke_WithResponse()
        {
            HttpRequestMessage request = new HttpRequestMessage();

            object result = await Execute(request).ConfigureAwait(false);
            
            HttpResponseMessage response = AssertIsType<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.AreEqual(request, response.RequestMessage);
        }
        private static HttpResponse Invoke_WithResponse_Target(IDatabaseAccessorFactory databaseAccessorFactory) => new HttpResponse(HttpStatusCode.Forbidden);

        [TestMethod]
        public async Task Invoke_DDL_WithHttpServerError_IsMappedToHttpStatusCode()
        {
            HttpRequestMessage request = new HttpRequestMessage();

            try
            {
                await Execute(request).ConfigureAwait(false);
            }
            catch (HttpRequestExecutionException requestException)
            {
                Assert.AreEqual(@"504 GatewayTimeout: Too late
CommandType: 0
CommandText: <Dynamic>", requestException.Message);
                AssertIsType<DatabaseAccessException>(requestException.InnerException);
                Assert.IsFalse(requestException.IsClientError);
                Assert.AreEqual(HttpStatusCode.GatewayTimeout, requestException.ErrorResponse.StatusCode);
                Assert.IsFalse(requestException.ErrorResponse.Headers.Contains("X-Error-Code"));
                Assert.IsFalse(requestException.ErrorResponse.Headers.Contains("X-Error-Description"));
                Assert.AreEqual(request, requestException.ErrorResponse.RequestMessage);
            }
        }
        private static void Invoke_DDL_WithHttpServerError_IsMappedToHttpStatusCode_Target(IDatabaseAccessorFactory databaseAccessorFactory) => throw CreateException(errorInfoNumber: 504000, errorMessage: "Too late");

        [TestMethod]
        public async Task Invoke_DDL_WithHttpClientError_IsMappedToHttpStatusCode()
        {
            HttpRequestMessage request = new HttpRequestMessage();

            try
            {
                await Execute(request).ConfigureAwait(false);
            }
            catch (HttpRequestExecutionException requestException)
            {
                Assert.AreEqual(@"403 Forbidden: Sorry
CommandType: 0
CommandText: <Dynamic>", requestException.Message);
                AssertIsType<DatabaseAccessException>(requestException.InnerException);
                Assert.IsTrue(requestException.IsClientError);
                Assert.AreEqual(HttpStatusCode.Forbidden, requestException.ErrorResponse.StatusCode);
                Assert.AreEqual("1", requestException.ErrorResponse.Headers.GetValues("X-Error-Code").Single());
                Assert.AreEqual("Sorry", requestException.ErrorResponse.Headers.GetValues("X-Error-Description").Single());
                Assert.AreEqual(request, requestException.ErrorResponse.RequestMessage);
            }
        }
        private static void Invoke_DDL_WithHttpClientError_IsMappedToHttpStatusCode_Target(IDatabaseAccessorFactory databaseAccessorFactory) => throw CreateException(errorInfoNumber: 403001, errorMessage: "Sorry");

        [TestMethod]
        public async Task Invoke_DDL_WithHttpClientError_ProducedByAuthorizationBehavior_IsMappedToHttpStatusCode()
        {
            HttpRequestMessage request = new HttpRequestMessage();

            try
            {
                await Execute(request).ConfigureAwait(false);
            }
            catch (HttpRequestExecutionException requestException)
            {
                Assert.AreEqual(@"403 Forbidden: Sorry
CommandType: 0
CommandText: <Dynamic>", requestException.Message);
                AssertIsType<DatabaseAccessException>(requestException.InnerException);
                Assert.IsTrue(requestException.IsClientError);
                Assert.AreEqual(HttpStatusCode.Forbidden, requestException.ErrorResponse.StatusCode);
                Assert.AreEqual("1", requestException.ErrorResponse.Headers.GetValues("X-Error-Code").Single());
                Assert.AreEqual("Sorry", requestException.ErrorResponse.Headers.GetValues("X-Error-Description").Single());
                Assert.AreEqual(request, requestException.ErrorResponse.RequestMessage);
            }
        }
        private static void Invoke_DDL_WithHttpClientError_ProducedByAuthorizationBehavior_IsMappedToHttpStatusCode_Target(IDatabaseAccessorFactory databaseAccessorFactory) { }
        private static void Invoke_DDL_WithHttpClientError_ProducedByAuthorizationBehavior_IsMappedToHttpStatusCode_Authorization_Target(IDatabaseAccessorFactory databaseAccessorFactory) => throw CreateException(errorInfoNumber: 403001, errorMessage: "Sorry");

        [TestMethod]
        public async Task Invoke_DDL_WithSqlException_WrappedExceptionIsThrown()
        {
            try
            {
                await Execute().ConfigureAwait(false);
                Assert.IsTrue(false, "DatabaseAccessException was expected but not thrown");
            }
            catch (DatabaseAccessException ex)
            {
                Assert.AreEqual(CommandType.StoredProcedure, ex.CommandType);
                Assert.AreEqual("x", ex.CommandText);
                Assert.AreEqual(@"Oops
CommandType: StoredProcedure
CommandText: x
Parameter a(Binary): System.Byte[]
Parameter b([dbo].[x]):
intValue INT(4)  stringValue NVARCHAR(MAX)
---------------  -------------------------
1                I                        
2                II                       ", ex.Message);
            }
        }
        private static void Invoke_DDL_WithSqlException_WrappedExceptionIsThrown_Target(IDatabaseAccessorFactory databaseAccessorFactory) => throw CreateException(errorInfoNumber: 50000, errorMessage: "Oops", CommandType.StoredProcedure, commandText: "x", visitParameter =>
        {
            visitParameter("a", DbType.Binary, new byte[] { 1 }, false, CustomInputType.None);
            visitParameter("b", DbType.Object, new X
            {
                { 1, "I" },
                { 2, "II" }
            }, false, CustomInputType.None);
        });

        [TestMethod]
        public async Task Invoke_DML_WithSqlException_WrappedExceptionIsThrown()
        {
            try
            {
                await Execute().ConfigureAwait(false);
                Assert.IsTrue(false, "DatabaseAccessException was expected but not thrown");
            }
            catch (DatabaseAccessException ex)
            {
                Assert.AreEqual(CommandType.Text, ex.CommandType);
                Assert.AreEqual("x", ex.CommandText);
                Assert.AreEqual(@"Oops
CommandType: Text
CommandText: <Dynamic>
Parameter a(Binary): System.Byte[]
Parameter b([dbo].[x]):
intValue INT(4)  stringValue NVARCHAR(MAX)
---------------  -------------------------
1                I                        
2                II                       ", ex.Message);
            }
        }
        private static void Invoke_DML_WithSqlException_WrappedExceptionIsThrown_Target(IDatabaseAccessorFactory databaseAccessorFactory) => throw CreateException(errorInfoNumber: default, errorMessage: "Oops", CommandType.Text, commandText: "x", (InputParameterVisitor visitParameter) =>
        {
            visitParameter("a", DbType.Binary, new byte[] { 1 }, false, CustomInputType.None);
            visitParameter("b", DbType.Object, new X
            {
                { 1, "I" },
                { 2, "II" }
            }, false, CustomInputType.None);
        });
    }
}