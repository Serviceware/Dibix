using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Dibix.Http.Server.AspNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Dibix.Http.Server.Tests
{
    [TestClass]
    public partial class HttpActionInvokerTest
    {
        [TestMethod]
        public async Task Invoke_WithResult()
        {
            object result = await CompileAndExecute().ConfigureAwait(false);
            Assert.AreEqual(1, result);
        }
        private static int Invoke_WithResult_Target(IDatabaseAccessorFactory databaseAccessorFactory) => 1;

        [TestMethod]
        public async Task Invoke_DDL_WithHttpServerError_IsMappedToHttpStatusCode()
        {
            HttpRequestMessage request = new HttpRequestMessage();

            try
            {
                await CompileAndExecute(request).ConfigureAwait(false);
                Assert.Fail($"{nameof(HttpRequestExecutionException)} was expected but not thrown");
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
                await CompileAndExecute(request).ConfigureAwait(false);
                Assert.Fail($"{nameof(HttpRequestExecutionException)} was expected but not thrown");
            }
            catch (HttpRequestExecutionException requestException)
            {
                HttpResponseMessage response = requestException.CreateResponse(request);
                Assert.AreEqual("""
                                403 Forbidden: Sorry
                                Somebody printed some stuff earlier using RAISERROR WITH NOWAIT
                                CommandType: 0
                                CommandText: <Inline>
                                """, requestException.Message);
                AssertIsType<DatabaseAccessException>(requestException.InnerException);
                Assert.IsTrue(requestException.IsClientError);
                Assert.AreEqual("""
                                StatusCode: 403, ReasonPhrase: 'Forbidden', Version: 1.1, Content: System.Net.Http.StringContent, Headers:
                                {
                                  Content-Type: application/problem+json; charset=utf-8
                                }
                                """, response.ToString());
                Assert.AreEqual("""
                                {
                                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                                  "title": "Forbidden",
                                  "status": 403,
                                  "code": 1,
                                  "detail": "Sorry"
                                }
                                """, await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
        }
        private static void Invoke_DDL_WithHttpClientError_IsMappedToHttpStatusCode_Target(IDatabaseAccessorFactory databaseAccessorFactory) => throw CreateException(errorInfoNumber: 403001, errorMessage: """
                                                                                                                                                                                                             Sorry
                                                                                                                                                                                                             Somebody printed some stuff earlier using RAISERROR WITH NOWAIT
                                                                                                                                                                                                             """);

        [TestMethod]
        public async Task Invoke_DDL_WithHttpClientError_ProducedByAuthorizationBehavior_IsMappedToHttpStatusCode()
        {
            Mock<IHttpRequestDescriptor> request = new Mock<IHttpRequestDescriptor>(MockBehavior.Strict);
            Mock<HttpResponseFormatter<IHttpRequestDescriptor>> responseFormatter = new Mock<HttpResponseFormatter<IHttpRequestDescriptor>>(MockBehavior.Strict);

            request.Setup(x => x.GetUser()).Returns(new ClaimsPrincipal(new ClaimsIdentity(EnumerableExtensions.Create(new Claim(ClaimTypes.NameIdentifier, "user")))));

            HttpActionDefinition action = Compile(authorizationConfiguration: x => x.ResolveParameterFromClaim("userid", ClaimTypes.NameIdentifier));
            Assert.HasCount(1, action.RequiredClaims);
            Assert.AreEqual(ClaimTypes.NameIdentifier, action.RequiredClaims[0], "action.RequiredClaims[0]");

            HttpAuthorizationBehaviorContext httpAuthorizationBehaviorContext = new HttpAuthorizationBehaviorContext();
            try
            {
                await Execute(action, request.Object, responseFormatter.Object, new KeyValuePair<string, object>("context", httpAuthorizationBehaviorContext)).ConfigureAwait(false);
                Assert.Fail($"{nameof(HttpRequestExecutionException)} was expected but not thrown");
            }
            catch (HttpRequestExecutionException requestException)
            {
                Assert.AreEqual("FirstAuthorizationTargetCalled", httpAuthorizationBehaviorContext.Result);
                Assert.AreEqual("""
                                403 Forbidden: Sorry
                                CommandType: 0
                                CommandText: <Inline>
                                """, requestException.Message);
                AssertIsType<DatabaseAccessException>(requestException.InnerException);
                Assert.IsTrue(requestException.IsClientError);
                Assert.AreEqual(HttpStatusCode.Forbidden, requestException.StatusCode);
            }
        }
        private static void Invoke_DDL_WithHttpClientError_ProducedByAuthorizationBehavior_IsMappedToHttpStatusCode_Target(IDatabaseAccessorFactory databaseAccessorFactory) { }
        private static void Invoke_DDL_WithHttpClientError_ProducedByAuthorizationBehavior_IsMappedToHttpStatusCode_Authorization_Target1(IDatabaseAccessorFactory databaseAccessorFactory, HttpAuthorizationBehaviorContext context) => context.Result = "FirstAuthorizationTargetCalled";
        private static void Invoke_DDL_WithHttpClientError_ProducedByAuthorizationBehavior_IsMappedToHttpStatusCode_Authorization_Target2(IDatabaseAccessorFactory databaseAccessorFactory, string userid) => throw CreateException(errorInfoNumber: 403001, errorMessage: "Sorry");

        [TestMethod]
        public async Task Invoke_DDL_WithHttpClientError_AutoDetectedByDatabaseErrorCode_AndProducedByThrow_IsMappedToHttpStatusCode()
        {
            HttpRequestMessage request = new HttpRequestMessage();

            try
            {
                await CompileAndExecute
                (
                    request
                  , x =>
                    {
                        x.Method = HttpApiMethod.Get;
                        x.SetStatusCodeDetectionResponse(404, 1, "The user '{name}' with the id '{id}' ({additionalId}) [{unmatchedParameter}] could not be found");
                    }
                  , _ => { }
                  , new KeyValuePair<string, object>("id", 666)
                  , new KeyValuePair<string, object>("name", "Darth")
                  , new KeyValuePair<string, object>("Name", "Darth2")
                  , new KeyValuePair<string, object>("additionalId", null)
                ).ConfigureAwait(false);
                Assert.Fail($"{nameof(HttpRequestExecutionException)} was expected but not thrown");
            }
            catch (HttpRequestExecutionException requestException)
            {
                HttpResponseMessage response = requestException.CreateResponse(request);
                Assert.AreEqual("""
                                404 NotFound: Sequence contains no elements
                                CommandType: Text
                                CommandText: <Inline>
                                """, requestException.Message);
                AssertIsType<DatabaseAccessException>(requestException.InnerException);
                Assert.IsTrue(requestException.IsClientError);
                Assert.AreEqual("""
                                StatusCode: 404, ReasonPhrase: 'Not Found', Version: 1.1, Content: System.Net.Http.StringContent, Headers:
                                {
                                  Content-Type: application/problem+json; charset=utf-8
                                }
                                """, response.ToString());
                Assert.AreEqual("""
                                {
                                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                                  "title": "Not Found",
                                  "status": 404,
                                  "code": 1,
                                  "detail": "The user 'Darth' with the id '666' () [{unmatchedParameter}] could not be found"
                                }
                                """, await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                Assert.AreEqual(request, response.RequestMessage);
            }
        }
        private static void Invoke_DDL_WithHttpClientError_AutoDetectedByDatabaseErrorCode_AndProducedByThrow_IsMappedToHttpStatusCode_Target(IDatabaseAccessorFactory databaseAccessorFactory, int id, string name) => throw CreateException(DatabaseAccessErrorCode.SequenceContainsNoElements, errorMessage: "Sequence contains no elements", CommandType.Text, commandText: "x");

        [TestMethod]
        public async Task Invoke_DDL_WithHttpClientError_AutoDetectedByDatabaseErrorCode_IsMappedToHttpStatusCode()
        {
            HttpRequestMessage request = new HttpRequestMessage();

            try
            {
                await CompileAndExecute
                (
                    request
                  , actionConfiguration: null
                  , authorizationConfiguration: null
                  , new KeyValuePair<string, object>("id", 666)
                  , new KeyValuePair<string, object>("name", "Darth")
                  , new KeyValuePair<string, object>("Name", "Darth2")
                  , new KeyValuePair<string, object>("additionalId", null)
                ).ConfigureAwait(false);
                Assert.Fail($"{nameof(HttpRequestExecutionException)} was expected but not thrown");
            }
            catch (HttpRequestExecutionException requestException)
            {
                HttpResponseMessage response = requestException.CreateResponse(request);
                Assert.AreEqual("""
                                404 NotFound: Sequence contains no elements
                                CommandType: Text
                                CommandText: <Inline>
                                """, requestException.Message);
                AssertIsType<DatabaseAccessException>(requestException.InnerException);
                Assert.IsTrue(requestException.IsClientError);
                Assert.AreEqual("""
                                StatusCode: 404, ReasonPhrase: 'Not Found', Version: 1.1, Content: System.Net.Http.StringContent, Headers:
                                {
                                  Content-Type: application/problem+json; charset=utf-8
                                }
                                """, response.ToString());
                Assert.AreEqual("""
                                {
                                  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                                  "title": "Not Found",
                                  "status": 404,
                                  "code": 0,
                                  "detail": "The entity could not be found"
                                }
                                """, await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                Assert.AreEqual(request, response.RequestMessage);
            }
        }
        private static void Invoke_DDL_WithHttpClientError_AutoDetectedByDatabaseErrorCode_IsMappedToHttpStatusCode_Target(IDatabaseAccessorFactory databaseAccessorFactory, int id, string name) => throw CreateException(DatabaseAccessErrorCode.SequenceContainsNoElements, errorMessage: "Sequence contains no elements", CommandType.Text, commandText: "x");

        [TestMethod]
        public async Task Invoke_DDL_WithSqlException_WrappedExceptionIsThrown()
        {
            try
            {
                await CompileAndExecute().ConfigureAwait(false);
                Assert.Fail($"{nameof(DatabaseAccessException)} was expected but not thrown");
            }
            catch (DatabaseAccessException ex)
            {
                Assert.AreEqual(DatabaseAccessErrorCode.None, ex.AdditionalErrorCode);
                Assert.AreEqual(CommandType.StoredProcedure, ex.CommandType);
                Assert.AreEqual("x", ex.CommandText);
                Assert.AreEqual("""
                                Oops
                                CommandType: StoredProcedure
                                CommandText: x
                                """, ex.Message);
                Assert.AreEqual("""
                                Parameter a Binary: System.Byte[]
                                Parameter b x:
                                intValue INT(4)  stringValue NVARCHAR(MAX)                  anotherStringValue NVARCHAR(MAX)
                                ---------------  -----------------------------------------  --------------------------------
                                1                I                                          A                               
                                2                This value is longer than the column name  B                               
                                Parameter c String(5): value
                                """, ex.ParameterDump);
                Assert.AreEqual("""
                                DECLARE @a VARBINARY(MAX) = 0x01
                                DECLARE @b [x]
                                DECLARE @c NVARCHAR(5)    = N'value'
                                INSERT INTO @b ([intValue], [stringValue],                                [anotherStringValue])
                                        VALUES (1,          N'I',                                         N'A')
                                             , (2,          N'This value is longer than the column name', N'B')

                                EXEC x @a = @a
                                     , @b = @b
                                     , @c = @c
                                """, ex.TSqlDebugStatement);
                Assert.AreEqual("""
                                Dibix.DatabaseAccessException: Oops
                                CommandType: StoredProcedure
                                CommandText: x

                                DECLARE @a VARBINARY(MAX) = 0x01
                                DECLARE @b [x]
                                DECLARE @c NVARCHAR(5)    = N'value'
                                INSERT INTO @b ([intValue], [stringValue],                                [anotherStringValue])
                                        VALUES (1,          N'I',                                         N'A')
                                             , (2,          N'This value is longer than the column name', N'B')

                                EXEC x @a = @a
                                     , @b = @b
                                     , @c = @c
                                """, GetExceptionTextWithoutCallStack(ex));
            }
        }
        private static void Invoke_DDL_WithSqlException_WrappedExceptionIsThrown_Target(IDatabaseAccessorFactory databaseAccessorFactory) => throw CreateException(errorInfoNumber: 50000, errorMessage: "Oops", CommandType.StoredProcedure, commandText: "x", visitParameter =>
        {
            visitParameter("a", DbType.Binary, new byte[] { 1 }, size: null, isOutput: false, CustomInputType.None);
            visitParameter("b", DbType.Object, new X
            {
                { 1, "I", "A" },
                { 2, "This value is longer than the column name", "B" }
            }, size: null, isOutput: false, CustomInputType.None);
            visitParameter("c", DbType.String, "value", size: 5, isOutput: false, CustomInputType.None);
        });

        [TestMethod]
        public async Task Invoke_DML_WithSqlException_WrappedExceptionIsThrown()
        {
            try
            {
                await CompileAndExecute().ConfigureAwait(false);
                Assert.Fail($"{nameof(DatabaseAccessException)} was expected but not thrown");
            }
            catch (DatabaseAccessException ex)
            {
                Assert.AreEqual(DatabaseAccessErrorCode.None, ex.AdditionalErrorCode);
                Assert.AreEqual(CommandType.Text, ex.CommandType);
                Assert.AreEqual("x", ex.CommandText);
                Assert.AreEqual("""
                                Oops
                                CommandType: Text
                                CommandText: <Inline>
                                """, ex.Message);
                Assert.AreEqual("""
                                Parameter a Binary: System.Byte[]
                                Parameter b x:
                                intValue INT(4)  stringValue NVARCHAR(MAX)                  anotherStringValue NVARCHAR(MAX)
                                ---------------  -----------------------------------------  --------------------------------
                                1                I                                          A                               
                                2                This value is longer than the column name  B                               
                                Parameter c String(5): value
                                """, ex.ParameterDump);
                Assert.AreEqual("""
                                DECLARE @a VARBINARY(MAX) = 0x01
                                DECLARE @b [x]
                                DECLARE @c NVARCHAR(5)    = N'value'
                                INSERT INTO @b ([intValue], [stringValue],                                [anotherStringValue])
                                        VALUES (1,          N'I',                                         N'A')
                                             , (2,          N'This value is longer than the column name', N'B')
                                """, ex.TSqlDebugStatement);
                Assert.AreEqual("""
                                Dibix.DatabaseAccessException: Oops
                                CommandType: Text
                                CommandText: <Inline>

                                DECLARE @a VARBINARY(MAX) = 0x01
                                DECLARE @b [x]
                                DECLARE @c NVARCHAR(5)    = N'value'
                                INSERT INTO @b ([intValue], [stringValue],                                [anotherStringValue])
                                        VALUES (1,          N'I',                                         N'A')
                                             , (2,          N'This value is longer than the column name', N'B')
                                """, GetExceptionTextWithoutCallStack(ex));
            }
        }
        private static void Invoke_DML_WithSqlException_WrappedExceptionIsThrown_Target(IDatabaseAccessorFactory databaseAccessorFactory) => throw CreateException(errorInfoNumber: default, errorMessage: "Oops", CommandType.Text, commandText: "x", (InputParameterVisitor visitParameter) =>
        {
            visitParameter("a", DbType.Binary, new byte[] { 1 }, size: null, isOutput: false, CustomInputType.None);
            visitParameter("b", DbType.Object, new X
            {
                { 1, "I", "A" },
                { 2, "This value is longer than the column name", "B" }
            }, size: null, isOutput: false, CustomInputType.None);
            visitParameter("c", DbType.String, "value", size: 5, isOutput: false, CustomInputType.None);
        });
    }
}
