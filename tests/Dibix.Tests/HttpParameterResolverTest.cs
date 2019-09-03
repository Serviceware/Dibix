using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Dibix.Http;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Dibix.Tests
{
    public partial class HttpParameterResolverTest
    {
        [Fact]
        public void Compile_Default()
        {
            IHttpParameterResolutionMethod result = Compile();
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

        [Fact]
        public void Compile_PropertySource()
        {
            HttpParameterSourceProviderRegistry.Register<HttpParameterSourceProvider>("SESSION");
            IHttpParameterResolutionMethod result = Compile(x => x.ResolveParameter("lcid", "SESSION", "LocaleId"));
            Assert.Equal(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactory,
        Dibix.Tests.HttpParameterResolverTest+HttpParameterSource $session) {
        $databaseaccessorfactory = .Call $dependencyResolver.Resolve();
        $session = .New Dibix.Tests.HttpParameterResolverTest+HttpParameterSource();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactory);
        .Call $arguments.Add(
            ""lcid"",
            (System.Object)$session.LocaleId)
    }
}", result.Source);
            Assert.False(result.Parameters.Any());

            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(arguments, dependencyResolver.Object);

            Assert.Equal(2, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.Equal(1033, arguments["lcid"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }

        [Fact]
        public void Compile_BodyConverter()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.BodyContract = typeof(JObject);
                x.ResolveParameter("data", typeof(JsonToXmlConverter).AssemblyQualifiedName);
            });
            Assert.Equal(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactory) {
        $databaseaccessorfactory = .Call $dependencyResolver.Resolve();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactory);
        .Call Dibix.Http.HttpParameterResolver.AddParameterFromBody(
            $arguments,
            ""data"")
    }
}", result.Source);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal(typeof(JObject), result.Parameters["$body"]);

            object body = JObject.Parse("{\"id\":5}");
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "$body", body } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(arguments, dependencyResolver.Object);

            Assert.Equal(3, arguments.Count);
            Assert.Equal(body, arguments["$body"]);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.Equal("<id>5</id>", arguments["data"].ToString());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }

        private static void Compile_Default_Target(IDatabaseAccessorFactory databaseAccessorFactory) { }

        private static void Compile_PropertySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, int lcid) { }

        private static void Compile_BodyConverter_Target(IDatabaseAccessorFactory databaseAccessorFactory, XElement data) { }
    }
}
