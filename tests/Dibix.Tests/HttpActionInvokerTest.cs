using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Dibix.Http;
using Moq;
using Xunit;

namespace Dibix.Tests
{
    public class HttpActionInvokerTest
    {
        [Fact]
        public async Task Invoke_WithResult()
        {
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            parameterResolver.Setup(x => x.PrepareParameters(null, null, null));

            Func<Task<object>> executor = () => Task.FromResult<object>(1);

            object result = await HttpActionInvoker.Invoke(null, null, null, parameterResolver.Object, executor, null).ConfigureAwait(false);
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Invoke_WithResponse()
        {
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            HttpRequestMessage request = new HttpRequestMessage();
            parameterResolver.Setup(x => x.PrepareParameters(request, null, null));

            Func<Task<object>> executor = () => Task.FromResult<object>(new HttpResponse(HttpStatusCode.Forbidden));

            object result = await HttpActionInvoker.Invoke(null, request, null, parameterResolver.Object, executor, null).ConfigureAwait(false);
            HttpResponseMessage response = Assert.IsType<HttpResponseMessage>(result);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(request, response.RequestMessage);
        }

        [Fact]
        public async Task Invoke_DDL_WithHttpServerError_IsMappedToHttpStatusCode()
        {
            SqlException sqlException = SqlExceptionFactory.Create(default, 504000, default, default, default, "Too late", default, default);

            Mock<IParametersVisitor> parametersVisitor = new Mock<IParametersVisitor>(MockBehavior.Strict);
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            parametersVisitor.Setup(x => x.VisitInputParameters(It.IsAny<InputParameterVisitor>()));

            HttpRequestMessage request = new HttpRequestMessage();
            Exception exception = (Exception)typeof(DatabaseAccessException).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { null, null, parametersVisitor.Object, sqlException });
            parameterResolver.Setup(x => x.PrepareParameters(request, null, null)).Throws(exception);

            try
            {
                await HttpActionInvoker.Invoke(null, request, null, parameterResolver.Object, null, null).ConfigureAwait(false);
            }
            catch (HttpRequestExecutionException requestException)
            {
                Assert.Equal(@"504 GatewayTimeout: Too late
CommandType: 0
CommandText: <Dynamic>", requestException.Message);
                Assert.IsType<DatabaseAccessException>(requestException.InnerException);
                Assert.False(requestException.IsClientError);
                Assert.Equal(HttpStatusCode.GatewayTimeout, requestException.ErrorResponse.StatusCode);
                Assert.False(requestException.ErrorResponse.Headers.Contains("X-Error-Code"));
                Assert.False(requestException.ErrorResponse.Headers.Contains("X-Error-Description"));
                Assert.Equal(request, requestException.ErrorResponse.RequestMessage);
            }
        }

        [Fact]
        public async Task Invoke_DDL_WithHttpClientError_IsMappedToHttpStatusCode()
        {
            SqlException sqlException = SqlExceptionFactory.Create(default, 403001, default, default, default, "Sorry", default, default);

            Mock<IParametersVisitor> parametersVisitor = new Mock<IParametersVisitor>(MockBehavior.Strict);
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            parametersVisitor.Setup(x => x.VisitInputParameters(It.IsAny<InputParameterVisitor>()));

            HttpRequestMessage request = new HttpRequestMessage();
            Exception exception = (Exception)typeof(DatabaseAccessException).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { null, null, parametersVisitor.Object, sqlException });
            parameterResolver.Setup(x => x.PrepareParameters(request, null, null)).Throws(exception);

            try
            {
                await HttpActionInvoker.Invoke(null, request, null, parameterResolver.Object, null, null).ConfigureAwait(false);
            }
            catch (HttpRequestExecutionException requestException)
            {
                Assert.Equal(@"403 Forbidden: Sorry
CommandType: 0
CommandText: <Dynamic>", requestException.Message);
                Assert.IsType<DatabaseAccessException>(requestException.InnerException);
                Assert.True(requestException.IsClientError);
                Assert.Equal(HttpStatusCode.Forbidden, requestException.ErrorResponse.StatusCode);
                Assert.Equal("1", requestException.ErrorResponse.Headers.GetValues("X-Error-Code").Single());
                Assert.Equal("Sorry", requestException.ErrorResponse.Headers.GetValues("X-Error-Description").Single());
                Assert.Equal(request, requestException.ErrorResponse.RequestMessage);
            }
        }

        [Fact]
        public async Task Invoke_DDL_WithSqlException_WrappedExceptionIsThrown()
        {
            CommandType commandType = CommandType.StoredProcedure;
            string commandText = "x";
            SqlException sqlException = SqlExceptionFactory.Create(default, 50000, default, default, default, "Oops", default, default);

            Mock<IParametersVisitor> parametersVisitor = new Mock<IParametersVisitor>(MockBehavior.Strict);
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            parametersVisitor.Setup(x => x.VisitInputParameters(It.IsAny<InputParameterVisitor>()))
                             .Callback((InputParameterVisitor visitParameter) =>
                             {
                                 visitParameter("a", DbType.Binary, new byte[] { 1 }, false);
                                 visitParameter("b", DbType.Object, new X
                                 {
                                     { 1, "I" },
                                     { 2, "II" }
                                 }, false);
                             });

            Exception exception = (Exception)typeof(DatabaseAccessException).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { commandType, commandText, parametersVisitor.Object, sqlException });
            parameterResolver.Setup(x => x.PrepareParameters(null, null, null)).Throws(exception);

            try
            {
                await HttpActionInvoker.Invoke(null, null, null, parameterResolver.Object, null, null).ConfigureAwait(false);
                Assert.True(false, "DatabaseAccessException was expected but not thrown");
            }
            catch (DatabaseAccessException ex)
            {
                Assert.Equal(CommandType.StoredProcedure, ex.CommandType);
                Assert.Equal("x", ex.CommandText);
                Assert.Equal(parametersVisitor.Object, ex.Parameters);
                Assert.Equal(@"Oops
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

        [Fact]
        public async Task Invoke_DML_WithSqlException_WrappedExceptionIsThrown()
        {
            CommandType commandType = CommandType.Text;
            string commandText = "x";
            SqlException sqlException = SqlExceptionFactory.Create(default, default, default, default, default, "Oops", default, default);

            Mock<IParametersVisitor> parametersVisitor = new Mock<IParametersVisitor>(MockBehavior.Strict);
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            parametersVisitor.Setup(x => x.VisitInputParameters(It.IsAny<InputParameterVisitor>()))
                             .Callback((InputParameterVisitor visitParameter) =>
                             {
                                 visitParameter("a", DbType.Binary, new byte[] { 1 }, false);
                                 visitParameter("b", DbType.Object, new X
                                 {
                                     { 1, "I" },
                                     { 2, "II" }
                                 }, false);
                             });

            Exception exception = (Exception)typeof(DatabaseAccessException).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { commandType, commandText, parametersVisitor.Object, sqlException });
            parameterResolver.Setup(x => x.PrepareParameters(null, null, null)).Throws(exception);

            try
            {
                await HttpActionInvoker.Invoke(null, null, null, parameterResolver.Object, null, null).ConfigureAwait(false);
                Assert.True(false, "DatabaseAccessException was expected but not thrown");
            }
            catch (DatabaseAccessException ex)
            {
                Assert.Equal(CommandType.Text, ex.CommandType);
                Assert.Equal("x", ex.CommandText);
                Assert.Equal(parametersVisitor.Object, ex.Parameters);
                Assert.Equal(@"Oops
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
    }

    
}