using System;
using System.Data;
using System.Data.SqlClient;
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
        public async Task Invoke_Empty()
        {
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            HttpRequestMessage request = new HttpRequestMessage();
            parameterResolver.Setup(x => x.PrepareParameters(request, null, null));

            Func<Task<object>> executor = () => Task.FromResult<object>(null);

            object result = await HttpActionInvoker.Invoke(null, request, null, parameterResolver.Object, executor, null).ConfigureAwait(false);
            HttpResponseMessage response = Assert.IsType<HttpResponseMessage>(result);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(request, response.RequestMessage);
        }

        [Fact]
        public async Task Invoke_DDL_WithCustomSqlException_IsMappedToHttpStatusCode()
        {
            SqlException sqlException = SqlExceptionFactory.Create(default, 50403, default, default, default, default, default, default);

            Mock<IParametersVisitor> parametersVisitor = new Mock<IParametersVisitor>(MockBehavior.Strict);
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            parametersVisitor.Setup(x => x.VisitParameters(It.IsAny<ParameterVisitor>()));

            HttpRequestMessage request = new HttpRequestMessage();
            Exception exception = (Exception)typeof(DatabaseAccessException).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { null, null, parametersVisitor.Object, sqlException });
            parameterResolver.Setup(x => x.PrepareParameters(request, null, null)).Throws(exception);

            object result = await HttpActionInvoker.Invoke(null, request, null, parameterResolver.Object, null, null).ConfigureAwait(false);
            HttpResponseMessage response = Assert.IsType<HttpResponseMessage>(result);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(request, response.RequestMessage);
        }

        [Fact]
        public async Task Invoke_DDL_WithSqlException_WrappedExceptionIsThrown()
        {
            CommandType commandType = CommandType.StoredProcedure;
            string commandText = "x";
            SqlException sqlException = SqlExceptionFactory.Create(default, default, default, default, default, "Oops", default, default);

            Mock<IParametersVisitor> parametersVisitor = new Mock<IParametersVisitor>(MockBehavior.Strict);
            Mock<IHttpParameterResolutionMethod> parameterResolver = new Mock<IHttpParameterResolutionMethod>(MockBehavior.Strict);

            parametersVisitor.Setup(x => x.VisitParameters(It.IsAny<ParameterVisitor>()))
                             .Callback((ParameterVisitor visitParameter) =>
                             {
                                 visitParameter("a", new byte[] { 1 }, typeof(byte[]), DbType.Binary);
                                 visitParameter("b", new X
                                 {
                                     { 1, "I" },
                                     { 2, "II" }
                                 }, typeof(StructuredType), null);
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
Parameter @a BINARY: System.Byte[]
Parameter @b [dbo].[x]
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

            parametersVisitor.Setup(x => x.VisitParameters(It.IsAny<ParameterVisitor>()))
                             .Callback((ParameterVisitor visitParameter) =>
                             {
                                 visitParameter("a", new byte[] { 1 }, typeof(byte[]), DbType.Binary);
                                 visitParameter("b", new X
                                 {
                                     { 1, "I" },
                                     { 2, "II" }
                                 }, typeof(StructuredType), null);
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
Parameter @a BINARY: System.Byte[]
Parameter @b [dbo].[x]
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