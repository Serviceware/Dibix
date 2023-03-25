﻿using System.Data;
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
                HttpResponseMessage response = requestException.CreateResponse(request);
                Assert.AreEqual(@"504 GatewayTimeout: Too late
CommandType: 0
CommandText: <Inline>", requestException.Message);
                AssertIsType<DatabaseAccessException>(requestException.InnerException);
                Assert.IsFalse(requestException.IsClientError);
                Assert.AreEqual(HttpStatusCode.GatewayTimeout, response.StatusCode);
                Assert.IsFalse(response.Headers.Contains("X-Error-Code"));
                Assert.IsFalse(response.Headers.Contains("X-Error-Description"));
                Assert.AreEqual(request, response.RequestMessage);
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
                HttpResponseMessage response = requestException.CreateResponse(request);
                Assert.AreEqual(@"403 Forbidden: Sorry
CommandType: 0
CommandText: <Inline>", requestException.Message);
                AssertIsType<DatabaseAccessException>(requestException.InnerException);
                Assert.IsTrue(requestException.IsClientError);
                Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.AreEqual("1", response.Headers.GetValues("X-Error-Code").Single());
                Assert.AreEqual("Sorry", response.Headers.GetValues("X-Error-Description").Single());
                Assert.AreEqual(request, response.RequestMessage);
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
                HttpResponseMessage response = requestException.CreateResponse(request);
                Assert.AreEqual(@"403 Forbidden: Sorry
CommandType: 0
CommandText: <Inline>", requestException.Message);
                AssertIsType<DatabaseAccessException>(requestException.InnerException);
                Assert.IsTrue(requestException.IsClientError);
                Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.AreEqual("1", response.Headers.GetValues("X-Error-Code").Single());
                Assert.AreEqual("Sorry", response.Headers.GetValues("X-Error-Description").Single());
                Assert.AreEqual(request, response.RequestMessage);
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
CommandText: x", ex.Message);
                Assert.AreEqual(@"Parameter a(Binary): System.Byte[]
Parameter b(x):
intValue INT(4)  stringValue NVARCHAR(MAX)
---------------  -------------------------
1                I                        
2                II                       ", ex.ParameterDump);
                Assert.AreEqual(@"DECLARE @a VARBINARY(MAX) = 0x01
DECLARE @b [x]
INSERT INTO @b ([intValue], [stringValue])
        VALUES (1         , N'I'         )
             , (2         , N'II'        )

EXEC x @a = @a
     , @b = @b", ex.SqlDebugStatement);
                Assert.AreEqual(@"Dibix.DatabaseAccessException: Oops
CommandType: StoredProcedure
CommandText: x

DECLARE @a VARBINARY(MAX) = 0x01
DECLARE @b [x]
INSERT INTO @b ([intValue], [stringValue])
        VALUES (1         , N'I'         )
             , (2         , N'II'        )

EXEC x @a = @a
     , @b = @b", GetExceptionTextWithoutCallStack(ex));
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
CommandText: <Inline>", ex.Message);
                Assert.AreEqual(@"Parameter a(Binary): System.Byte[]
Parameter b(x):
intValue INT(4)  stringValue NVARCHAR(MAX)
---------------  -------------------------
1                I                        
2                II                       ", ex.ParameterDump);
                Assert.AreEqual(@"DECLARE @a VARBINARY(MAX) = 0x01
DECLARE @b [x]
INSERT INTO @b ([intValue], [stringValue])
        VALUES (1         , N'I'         )
             , (2         , N'II'        )", ex.SqlDebugStatement);
                Assert.AreEqual(@"Dibix.DatabaseAccessException: Oops
CommandType: Text
CommandText: <Inline>

DECLARE @a VARBINARY(MAX) = 0x01
DECLARE @b [x]
INSERT INTO @b ([intValue], [stringValue])
        VALUES (1         , N'I'         )
             , (2         , N'II'        )", GetExceptionTextWithoutCallStack(ex));
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