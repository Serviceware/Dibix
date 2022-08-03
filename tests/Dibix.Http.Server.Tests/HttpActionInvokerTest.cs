using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Dibix.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Dibix.Http.Server.Tests
{
    [TestClass]
    public class HttpActionInvokerTest : TestBase
    {
        [TestMethod]
        public async Task Invoke_WithResult()
        {
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            parameterResolver.Setup(x => x.PrepareParameters(null, null, null));

            Func<Task<object>> executor = () => Task.FromResult<object>(1);

            HttpActionDefinition action = HttpActionDefinitionFactory.Create();
            object result = await HttpActionInvoker.Invoke(action, null, null, parameterResolver.Object, executor, null).ConfigureAwait(false);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public async Task Invoke_WithResponse()
        {
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            HttpRequestMessage request = new HttpRequestMessage();
            parameterResolver.Setup(x => x.PrepareParameters(request, null, null));

            Func<Task<object>> executor = () => Task.FromResult<object>(new HttpResponse(HttpStatusCode.Forbidden));

            HttpActionDefinition action = HttpActionDefinitionFactory.Create();
            object result = await HttpActionInvoker.Invoke(action, request, null, parameterResolver.Object, executor, null).ConfigureAwait(false);
            HttpResponseMessage response = AssertIsType<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.AreEqual(request, response.RequestMessage);
        }

        [TestMethod]
        public async Task Invoke_DDL_WithHttpServerError_IsMappedToHttpStatusCode()
        {
            SqlException sqlException = SqlExceptionFactory.Create(default, 504000, default, default, default, "Too late", default, default);

            Mock<ParametersVisitor> parametersVisitor = new Mock<ParametersVisitor>(MockBehavior.Strict);
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            parametersVisitor.Setup(x => x.VisitInputParameters(It.IsAny<InputParameterVisitor>()));

            HttpRequestMessage request = new HttpRequestMessage();
            Exception exception = (Exception)typeof(DatabaseAccessException).SafeGetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { null, null, parametersVisitor.Object, sqlException });
            parameterResolver.Setup(x => x.PrepareParameters(request, null, null)).Throws(exception);

            try
            {
                await HttpActionInvoker.Invoke(null, request, null, parameterResolver.Object, null, null).ConfigureAwait(false);
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

        [TestMethod]
        public async Task Invoke_DDL_WithHttpClientError_IsMappedToHttpStatusCode()
        {
            SqlException sqlException = SqlExceptionFactory.Create(default, 403001, default, default, default, "Sorry", default, default);

            Mock<ParametersVisitor> parametersVisitor = new Mock<ParametersVisitor>(MockBehavior.Strict);
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            parametersVisitor.Setup(x => x.VisitInputParameters(It.IsAny<InputParameterVisitor>()));

            HttpRequestMessage request = new HttpRequestMessage();
            Exception exception = (Exception)typeof(DatabaseAccessException).SafeGetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { null, null, parametersVisitor.Object, sqlException });
            parameterResolver.Setup(x => x.PrepareParameters(request, null, null)).Throws(exception);

            try
            {
                await HttpActionInvoker.Invoke(null, request, null, parameterResolver.Object, null, null).ConfigureAwait(false);
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

        [TestMethod]
        public async Task Invoke_DDL_WithSqlException_WrappedExceptionIsThrown()
        {
            CommandType commandType = CommandType.StoredProcedure;
            string commandText = "x";
            SqlException sqlException = SqlExceptionFactory.Create(default, 50000, default, default, default, "Oops", default, default);

            Mock<ParametersVisitor> parametersVisitor = new Mock<ParametersVisitor>(MockBehavior.Strict);
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            parametersVisitor.Setup(x => x.VisitInputParameters(It.IsAny<InputParameterVisitor>()))
                             .Callback((InputParameterVisitor visitParameter) =>
                             {
                                 visitParameter("a", DbType.Binary, new byte[] { 1 }, false, CustomInputType.None);
                                 visitParameter("b", DbType.Object, new X
                                 {
                                     { 1, "I" },
                                     { 2, "II" }
                                 }, false, CustomInputType.None);
                             });

            Exception exception = (Exception)typeof(DatabaseAccessException).SafeGetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { commandType, commandText, parametersVisitor.Object, sqlException });
            parameterResolver.Setup(x => x.PrepareParameters(null, null, null)).Throws(exception);

            try
            {
                await HttpActionInvoker.Invoke(null, null, null, parameterResolver.Object, null, null).ConfigureAwait(false);
                Assert.IsTrue(false, "DatabaseAccessException was expected but not thrown");
            }
            catch (DatabaseAccessException ex)
            {
                Assert.AreEqual(CommandType.StoredProcedure, ex.CommandType);
                Assert.AreEqual("x", ex.CommandText);
                Assert.AreEqual(parametersVisitor.Object, ex.Parameters);
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

        [TestMethod]
        public async Task Invoke_DML_WithSqlException_WrappedExceptionIsThrown()
        {
            CommandType commandType = CommandType.Text;
            string commandText = "x";
            SqlException sqlException = SqlExceptionFactory.Create(default, default, default, default, default, "Oops", default, default);

            Mock<ParametersVisitor> parametersVisitor = new Mock<ParametersVisitor>(MockBehavior.Strict);
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            parametersVisitor.Setup(x => x.VisitInputParameters(It.IsAny<InputParameterVisitor>()))
                             .Callback((InputParameterVisitor visitParameter) =>
                             {
                                 visitParameter("a", DbType.Binary, new byte[] { 1 }, false, CustomInputType.None);
                                 visitParameter("b", DbType.Object, new X
                                 {
                                     { 1, "I" },
                                     { 2, "II" }
                                 }, false, CustomInputType.None);
                             });

            Exception exception = (Exception)typeof(DatabaseAccessException).SafeGetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { commandType, commandText, parametersVisitor.Object, sqlException });
            parameterResolver.Setup(x => x.PrepareParameters(null, null, null)).Throws(exception);

            try
            {
                await HttpActionInvoker.Invoke(null, null, null, parameterResolver.Object, null, null).ConfigureAwait(false);
                Assert.IsTrue(false, "DatabaseAccessException was expected but not thrown");
            }
            catch (DatabaseAccessException ex)
            {
                Assert.AreEqual(CommandType.Text, ex.CommandType);
                Assert.AreEqual("x", ex.CommandText);
                Assert.AreEqual(parametersVisitor.Object, ex.Parameters);
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

        private class X : StructuredType<X, int, string>
        {
            public X() : base("x") => base.ImportSqlMetadata(() => this.Add(default, default));

            public void Add(int intValue, string stringValue) => base.AddValues(intValue, stringValue);
        }

        private class HttpActionDefinitionFactory : HttpApiDescriptor
        {
            private HttpActionDefinition _action;

            private HttpActionDefinitionFactory() { }

            public static HttpActionDefinition Create()
            {
                HttpActionDefinitionFactory apiDescriptor = new HttpActionDefinitionFactory();
                apiDescriptor.Configure(null);
                return apiDescriptor._action;
            }

            public override void Configure(IHttpApiDiscoveryContext context)
            {
                base.RegisterController("X", x => x.AddAction(null, y => this._action = y));
            }
        }
    }

    
}