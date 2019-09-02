using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Dibix.Http;
using Moq;
using Xunit;

namespace Dibix.Tests
{
    public class HttpParameterResolverTest
    {
        [Fact]
        public void Compile_Default()
        {
            HttpApiRegistration registration = new HttpApiRegistration();
            registration.Configure();
            HttpActionDefinition action = registration.Controllers.Single().Actions.Single();
            MethodInfo method = action.Target.Build();
            IHttpParameterResolutionMethod result = HttpParameterResolver.Compile(action, method.GetParameters());
            Assert.Equal(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactory) {
        $databaseaccessorfactory = .Call $dependencyResolver.Resolve();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactory)
    }
}", result.Source);
            Assert.False(result.Parameters.Any());

            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(arguments, dependencyResolver.Object);

            Assert.Equal(1, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }

        private static void Compile_Default_Target(IDatabaseAccessorFactory databaseAccessorFactory) { }

        private sealed class HttpApiRegistration : HttpApiDescriptor
        {
            private readonly string _methodName = $"{new StackTrace().GetFrame(1).GetMethod().Name}_Target";

            public override void Configure() => base.RegisterController(null, x => x.AddAction(ReflectionHttpActionTarget.Create(typeof(HttpParameterResolverTest), this._methodName), y => { }));
        }
    }
}
