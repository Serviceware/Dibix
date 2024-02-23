using System;
using System.Linq;
using Dibix.Testing;

namespace Dibix.Http.Server.Tests
{
    public partial class HttpActionExecutorTest : TestBase
    {
        private void AssertEqual(string expected, string actual) => base.AssertEqual(expected, actual, extension: "txt");

        private IHttpActionExecutionMethod Compile() => Compile(_ => { });
        private IHttpActionExecutionMethod Compile(Action<IHttpActionDefinitionBuilder> actionConfiguration)
        {
            HttpApiRegistration registration = new HttpApiRegistration(base.TestContext.TestName, actionConfiguration) { AreaName = "Dibix" };
            registration.Configure(null);
            HttpActionDefinition action = registration.Controllers.Single().Actions.Single();
            IHttpActionExecutionMethod result = action.Executor;
            return result;
        }

        private sealed class HttpApiRegistration : HttpApiDescriptor
        {
            private readonly string _methodName;
            private readonly Action<IHttpActionDefinitionBuilder> _actionConfiguration;

            public HttpApiRegistration(string testName, Action<IHttpActionDefinitionBuilder> actionConfiguration)
            {
                _actionConfiguration = actionConfiguration;
                _methodName = $"{testName}_Target";
            }

            public override void Configure(IHttpApiDiscoveryContext context) => base.RegisterController("Test", x => x.AddAction(ReflectionHttpActionTarget.Create(typeof(HttpActionExecutorTest), _methodName), _actionConfiguration));
        }
    }
}