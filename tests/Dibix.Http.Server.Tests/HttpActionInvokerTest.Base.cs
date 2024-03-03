using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dibix.Testing;
using Moq;
using Moq.Language.Flow;

namespace Dibix.Http.Server.Tests
{
    public partial class HttpActionInvokerTest : TestBase
    {
        private async Task<object> CompileAndExecute(HttpRequestMessage request = null, Action<IHttpActionDefinitionBuilder> actionConfiguration = null, Action<IHttpAuthorizationBuilder> authorizationConfiguration = null, params KeyValuePair<string, object>[] parameters)
        {
            HttpActionDefinition action = Compile(actionConfiguration, authorizationConfiguration);
            return await Execute(action, request, parameters).ConfigureAwait(false);
        }

        private HttpActionDefinition Compile(Action<IHttpActionDefinitionBuilder> actionConfiguration = null, Action<IHttpAuthorizationBuilder> authorizationConfiguration = null)
        {
            HttpApiRegistration registration = new HttpApiRegistration(base.TestContext.TestName, actionConfiguration, authorizationConfiguration) { Metadata = { AreaName = "Dibix" } };
            registration.Configure(null);
            HttpActionDefinition action = registration.Controllers.Single().Actions.Single();
            return action;
        }

        private static async Task<object> Execute(HttpActionDefinition action, HttpRequestMessage request = null, params KeyValuePair<string, object>[] parameters)
        {
            Mock<IParameterDependencyResolver> parameterDependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);

            parameterDependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns<IDatabaseAccessorFactory>(null);

            IDictionary<string, object> arguments = new Dictionary<string, object> { ["databaseAccessorFactory"] = null };
            foreach (KeyValuePair<string, object> parameter in parameters)
                arguments.Add(parameter);

            object result = await HttpActionInvoker.Invoke(action, request, arguments, parameterDependencyResolver.Object, default).ConfigureAwait(false);
            return result;
        }

        private static async Task<object> Execute<TRequest>(HttpActionDefinition action, TRequest request, IHttpResponseFormatter<TRequest> responseFormatter, params KeyValuePair<string, object>[] parameters) where TRequest : IHttpRequestDescriptor
        {
            Mock<IParameterDependencyResolver> parameterDependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);

            parameterDependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns<IDatabaseAccessorFactory>(null);

            IDictionary<string, object> arguments = new Dictionary<string, object> { ["databaseAccessorFactory"] = null };
            foreach (KeyValuePair<string, object> parameter in parameters)
                arguments.Add(parameter);
            
            object result = await HttpActionInvoker.Invoke(action, request, responseFormatter, arguments, parameterDependencyResolver.Object, default).ConfigureAwait(false);
            return result;
        }

        private static Exception CreateException(int errorInfoNumber, string errorMessage, CommandType? commandType = null, string commandText = null, Action<InputParameterVisitor> inputParameterVisitor = null)
        {
            Mock<ParametersVisitor> parametersVisitor = new Mock<ParametersVisitor>(MockBehavior.Strict);
            ISetup<ParametersVisitor> parametersVisitorSetup = parametersVisitor.Setup(x => x.VisitInputParameters(It.IsAny<InputParameterVisitor>()));
            if (inputParameterVisitor != null)
                parametersVisitorSetup.Callback(inputParameterVisitor);

            SqlException sqlException = SqlExceptionFactory.Create(serverVersion: default, infoNumber: errorInfoNumber, errorState: default, errorClass: default, server: default, errorMessage, procedure: default, lineNumber: default);
            const bool isSqlClient = true;

            MethodInfo createMethod = typeof(DatabaseAccessException).SafeGetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static, new[] { typeof(CommandType), typeof(string), typeof(ParametersVisitor), typeof(Exception), typeof(bool) });
            Exception exception = (Exception)createMethod.Invoke(null, new object[] { commandType, commandText, parametersVisitor.Object, sqlException, isSqlClient });
            return exception;
        }
        private static Exception CreateException(DatabaseAccessErrorCode errorCode, string errorMessage, CommandType? commandType = null, string commandText = null)
        {
            ParametersVisitor parametersVisitor = ParametersVisitor.Empty;
            const bool isSqlClient = true;
            MethodInfo createMethod = typeof(DatabaseAccessException).SafeGetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static, new[] { typeof(string), typeof(CommandType), typeof(string), typeof(ParametersVisitor), typeof(DatabaseAccessErrorCode), typeof(bool) });
            Exception exception = (Exception)createMethod.Invoke(null, new object[] { errorMessage, commandType, commandText, parametersVisitor, errorCode, isSqlClient });
            return exception;
        }

        private static string GetExceptionTextWithoutCallStack(Exception exception)
        {
            string exceptionText = exception.ToString();
            string normalizedExceptionText = Regex.Replace(exceptionText, @"(?<CallStack> --->(.|\n)+)\nDebug statement:", String.Empty);
            return normalizedExceptionText;
        }

        private sealed class HttpApiRegistration : HttpApiDescriptor
        {
            private readonly string _methodName;
            private readonly Action<IHttpActionDefinitionBuilder> _actionConfiguration;

            public HttpApiRegistration(string testName, Action<IHttpActionDefinitionBuilder> configureActions, Action<IHttpAuthorizationBuilder> configureAuthorization)
            {
                _methodName = $"{testName}_Target";
                _actionConfiguration = builder =>
                {
                    configureActions?.Invoke(builder);

                    string authorizationMethodName = $"{testName}_Authorization_Target";
                    if (typeof(HttpActionInvokerTest).GetMethod(authorizationMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) != null)
                        builder.WithAuthorization(ReflectionHttpActionTarget.Create(typeof(HttpActionInvokerTest), authorizationMethodName), configureAuthorization ?? (_ => { }));
                };
            }

            public override void Configure(IHttpApiDiscoveryContext context) => base.RegisterController("Test", x => x.AddAction(ReflectionHttpActionTarget.Create(typeof(HttpActionInvokerTest), _methodName), _actionConfiguration));
        }

        private sealed class X : StructuredType<X, int, string>
        {
            public X() : base("x") => base.ImportSqlMetadata(() => Add(default, default));

            public void Add(int intValue, string stringValue) => base.AddValues(intValue, stringValue);
        }
    }
}