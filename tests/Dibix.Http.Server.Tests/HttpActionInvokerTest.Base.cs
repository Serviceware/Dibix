﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Dibix.Testing;
using Moq;
using Moq.Language.Flow;

namespace Dibix.Http.Server.Tests
{
    public partial class HttpActionInvokerTest : TestBase
    {
        private async Task<object> Execute(HttpRequestMessage request = null)
        {
            Mock<IParameterDependencyResolver> parameterDependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);

            parameterDependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns<IDatabaseAccessorFactory>(null);

            HttpApiRegistration registration = new HttpApiRegistration(base.TestContext.TestName);
            registration.Configure(null);
            HttpActionDefinition action = registration.Controllers.Single().Actions.Single();

            IDictionary<string, object> arguments = new Dictionary<string, object> { ["databaseAccessorFactory"] = null };
            object result = await HttpActionInvoker.Invoke(action, request, arguments, parameterDependencyResolver.Object).ConfigureAwait(false);
            return result;
        }

        private static Exception CreateException(int errorInfoNumber, string errorMessage, CommandType? commandType = null, string commandText = null, Action<InputParameterVisitor> inputParameterVisitor = null)
        {
            Mock<ParametersVisitor> parametersVisitor = new Mock<ParametersVisitor>(MockBehavior.Strict);
            ISetup<ParametersVisitor> parametersVisitorSetup = parametersVisitor.Setup(x => x.VisitInputParameters(It.IsAny<InputParameterVisitor>()));
            if (inputParameterVisitor != null)
                parametersVisitorSetup.Callback(inputParameterVisitor);

            SqlException sqlException = SqlExceptionFactory.Create(serverVersion: default, infoNumber: errorInfoNumber, errorState: default, errorClass: default, server: default, errorMessage, procedure: default, lineNumber: default);

            Exception exception = (Exception)typeof(DatabaseAccessException).SafeGetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { commandType, commandText, parametersVisitor.Object, sqlException });
            return exception;
        }

        private sealed class HttpApiRegistration : HttpApiDescriptor
        {
            private readonly string _methodName;
            private readonly Action<IHttpActionDefinitionBuilder> _actionConfiguration;

            public HttpApiRegistration(string testName)
            {
                _methodName = $"{testName}_Target";
                
                string authorizationMethodName = $"{testName}_Authorization_Target";
                if (typeof(HttpActionInvokerTest).GetMethod(authorizationMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) != null)
                    _actionConfiguration = x => x.WithAuthorization(ReflectionHttpActionTarget.Create(typeof(HttpActionInvokerTest), authorizationMethodName), _ => { });
                else
                    _actionConfiguration = _ => { };
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