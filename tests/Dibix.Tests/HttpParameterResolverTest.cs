using System;
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
        private static void Compile_Default_Target(IDatabaseAccessorFactory databaseAccessorFactory) { }

        [Fact]
        public void Compile_PropertySource()
        {
            HttpParameterSourceProviderRegistry.Register<LocaleParameterHttpSourceProvider>("LOCALE");
            IHttpParameterResolutionMethod result = Compile(x => x.ResolveParameter("lcid", "LOCALE", "LocaleId"));
            Assert.Equal(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactory,
        Dibix.Tests.HttpParameterResolverTest+LocaleHttpParameterSource $locale) {
        $databaseaccessorfactory = .Call $dependencyResolver.Resolve();
        $locale = .New Dibix.Tests.HttpParameterResolverTest+LocaleHttpParameterSource();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactory);
        .Call $arguments.Add(
            ""lcid"",
            (System.Object)$locale.LocaleId)
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
        private static void Compile_PropertySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, int lcid) { }

        [Fact]
        public void Compile_PropertySource_WithInvalidCast_Throws()
        {
            HttpParameterSourceProviderRegistry.Register<ApplicationHttpParameterSourceProvider>("APPLICATION");
            IHttpParameterResolutionMethod result = Compile(x => x.ResolveParameter("applicationId", "APPLICATION", "ApplicationId"));
            Assert.Equal(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactory,
        Dibix.Tests.HttpParameterResolverTest+ApplicationHttpParameterSource $application) {
        $databaseaccessorfactory = .Call $dependencyResolver.Resolve();
        $application = .New Dibix.Tests.HttpParameterResolverTest+ApplicationHttpParameterSource();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactory);
        .Call $arguments.Add(
            ""applicationId"",
            (System.Object).Call Dibix.Http.HttpParameterResolver.ConvertValue(
                ""applicationId"",
                $application.ApplicationId))
    }
}", result.Source);
            Assert.False(result.Parameters.Any());

            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            Exception exception = Assert.Throws<InvalidOperationException>(() => result.PrepareParameters(arguments, dependencyResolver.Object));
            Assert.Equal(@"Parameter mapping failed
Parameter: applicationId", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("Null object cannot be converted to a value type.", exception.InnerException.Message);
        }
        private static void Compile_PropertySource_WithInvalidCast_Throws_Target(IDatabaseAccessorFactory databaseAccessorFactory, byte applicationId) { }

        [Fact]
        public void Compile_PropertySource_WithUnknownSource_Throws()
        {
            Exception exception = Assert.Throws<InvalidOperationException>(() => Compile(x => x.ResolveParameter("lcid", "UNKNOWNSOURCE", "LocaleId")));
            Assert.Equal(@"Http parameter resolver compilation failed
at GET api/Dibix/Test
Parameter: lcid", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("Unknown source provider 'UNKNOWNSOURCE' for property 'LocaleId'", exception.InnerException.Message);
        }
        private static void Compile_PropertySource_WithUnknownSource_Throws_Target(IDatabaseAccessorFactory databaseAccessorFactory, int lcid) { }

        [Fact]
        public void Compile_ExplicitBodySource()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.BodyContract = typeof(ExplicitHttpBody);
                x.ResolveParameter("targetId", "BODY", "SourceId");
                x.ResolveParameter("lcid", "BODY", "LocaleId");
            });
            Assert.Equal(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactory,
        Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBody $body,
        Dibix.Tests.HttpParameterResolverTest+ExplicitHttpParameterInput $input) {
        $databaseaccessorfactory = .Call $dependencyResolver.Resolve();
        $body = .Call Dibix.Http.HttpParameterResolverUtility.ReadBody($arguments);
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactory);
        .Call $arguments.Add(
            ""lcid"",
            (System.Object)$body.LocaleId);
        $input = .New Dibix.Tests.HttpParameterResolverTest+ExplicitHttpParameterInput();
        $input.targetid = .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""targetid"",
            $body.SourceId);
        .Call $arguments.Add(
            ""input"",
            (System.Object)$input)
    }
}", result.Source);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal(typeof(ExplicitHttpBody), result.Parameters["$body"]);

            object body = new ExplicitHttpBody
            {
                SourceId = 7,
                LocaleId = 1033
            };
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "$body", body } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(arguments, dependencyResolver.Object);

            Assert.Equal(4, arguments.Count);
            Assert.Equal(body, arguments["$body"]);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.IsType<ExplicitHttpParameterInput>(arguments["input"]);
            Assert.Equal(7, ((ExplicitHttpParameterInput)arguments["input"]).targetid);
            Assert.Equal(1033, arguments["lcid"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_ExplicitBodySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpParameterInput input, int lcid) { }

        [Fact]
        public void Compile_ImplicitBodySource()
        {
            IHttpParameterResolutionMethod result = Compile(x => x.BodyContract = typeof(ImplicitHttpBody));
            Assert.Equal(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactory,
        Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBody $body,
        Dibix.Tests.HttpParameterResolverTest+ImplicitBodyHttpParameterInput $input) {
        $databaseaccessorfactory = .Call $dependencyResolver.Resolve();
        $body = .Call Dibix.Http.HttpParameterResolverUtility.ReadBody($arguments);
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactory);
        .Call $arguments.Add(
            ""userid"",
            (System.Object)$body.UserId);
        $input = .New Dibix.Tests.HttpParameterResolverTest+ImplicitBodyHttpParameterInput();
        $input.sourceid = .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""sourceid"",
            $body.SourceId);
        $input.localeid = $body.LocaleId;
        $input.fromuri = .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""fromuri"",
            $arguments.Item[""fromuri""]);
        .Call $arguments.Add(
            ""input"",
            (System.Object)$input)
    }
}", result.Source);
            Assert.Equal(3, result.Parameters.Count);
            Assert.Equal(typeof(ImplicitHttpBody), result.Parameters["$body"]);
            Assert.Equal(typeof(int), result.Parameters["id"]);
            Assert.Equal(typeof(int), result.Parameters["fromuri"]);

            object body = new ImplicitHttpBody
            {
                SourceId = 7,
                LocaleId = 1033,
                UserId = 5
            };
            IDictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "$body", body }
              , { "id", 2 }
              , { "fromuri", 3 }
            };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(arguments, dependencyResolver.Object);

            Assert.Equal(6, arguments.Count);
            Assert.Equal(2, arguments["id"]);
            Assert.Equal(5, arguments["userid"]);
            Assert.Equal(3, arguments["fromuri"]);
            Assert.Equal(body, arguments["$body"]);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.IsType<ImplicitBodyHttpParameterInput>(arguments["input"]);
            Assert.Equal(7, ((ImplicitBodyHttpParameterInput)arguments["input"]).sourceid);
            Assert.Equal(1033, ((ImplicitBodyHttpParameterInput)arguments["input"]).localeid);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_ImplicitBodySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, int id, [InputClass] ImplicitBodyHttpParameterInput input, int userid) { }

        [Fact]
        public void Compile_BodyConverter()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.BodyContract = typeof(JObject);
                x.ResolveParameter("data", typeof(JsonToXmlConverter).AssemblyQualifiedName);
                x.ResolveParameter("value", typeof(JsonToXmlConverter).AssemblyQualifiedName);
            });
            Assert.Equal(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactory,
        Dibix.Tests.HttpParameterResolverTest+XmlHttpParameterInput $input) {
        $databaseaccessorfactory = .Call $dependencyResolver.Resolve();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactory);
        .Call Dibix.Http.HttpParameterResolver.AddParameterFromBody(
            $arguments,
            ""value"");
        $input = .New Dibix.Tests.HttpParameterResolverTest+XmlHttpParameterInput();
        $input.data = .Call Dibix.Http.HttpParameterResolver.ConvertParameterFromBody($arguments);
        .Call $arguments.Add(
            ""input"",
            (System.Object)$input)
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

            Assert.Equal(4, arguments.Count);
            Assert.Equal(body, arguments["$body"]);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.Equal("<id>5</id>", arguments["value"].ToString());
            Assert.IsType<XmlHttpParameterInput>(arguments["input"]);
            Assert.Equal("<id>5</id>", ((XmlHttpParameterInput)arguments["input"]).data.ToString());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_BodyConverter_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] XmlHttpParameterInput input, XElement value) { }

        [Fact]
        public void Compile_BodyBinder()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.BodyContract = typeof(ExplicitHttpBody);
                x.BodyBinder = typeof(FormattedInputBinder);
            });
            Assert.Equal(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactory,
        Dibix.Tests.HttpParameterResolverTest+ExplicitHttpParameterInput $input) {
        $databaseaccessorfactory = .Call $dependencyResolver.Resolve();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactory);
        $input = .New Dibix.Tests.HttpParameterResolverTest+ExplicitHttpParameterInput();
        .Call Dibix.Http.HttpParameterResolver.BindParametersFromBody(
            $arguments,
            $input);
        .Call $arguments.Add(
            ""input"",
            (System.Object)$input)
    }
}", result.Source);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal(typeof(ExplicitHttpBody), result.Parameters["$body"]);

            object body = new ExplicitHttpBody { SourceId = 7 };
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "$body", body } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(arguments, dependencyResolver.Object);

            Assert.Equal(3, arguments.Count);
            Assert.Equal(body, arguments["$body"]);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.IsType<ExplicitHttpParameterInput>(arguments["input"]);
            Assert.Equal(7, ((ExplicitHttpParameterInput)arguments["input"]).targetid);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_BodyBinder_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpParameterInput input) { }

        [Fact]
        public void Compile_BodyBinder_WithoutInputClass_Throws()
        {
            Exception exception = Assert.Throws<InvalidOperationException>(() => Compile(x =>
            {
                x.BodyContract = typeof(ExplicitHttpBody);
                x.BodyBinder = typeof(FormattedInputBinder);
            }));
            Assert.Equal(@"Http parameter resolver compilation failed
at GET api/Dibix/Test
Parameter: input", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("Using a binder for the body is only supported if the target parameter is a class and is marked with the Dibix.InputClassAttribute", exception.InnerException.Message);
        }
        private static void Compile_BodyBinder_WithoutInputClass_Throws_Target(IDatabaseAccessorFactory databaseAccessorFactory, ExplicitHttpParameterInput input) { }

        [Fact]
        public void Compile_ConstantSource()
        {
            IHttpParameterResolutionMethod result = Compile(x => x.ResolveParameter("value", true));
            Assert.Equal(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactory) {
        $databaseaccessorfactory = .Call $dependencyResolver.Resolve();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactory);
        .Call $arguments.Add(
            ""value"",
            (System.Object)True)
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
            Assert.Equal(true, arguments["value"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_ConstantSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, bool value) { }

        [Fact]
        public void Compile_UriSource()
        {
            IHttpParameterResolutionMethod result = Compile();
            Assert.Equal(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactory,
        Dibix.Tests.HttpParameterResolverTest+ExplicitHttpParameterInput $input) {
        $databaseaccessorfactory = .Call $dependencyResolver.Resolve();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactory);
        $input = .New Dibix.Tests.HttpParameterResolverTest+ExplicitHttpParameterInput();
        $input.targetid = .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""targetid"",
            $arguments.Item[""targetid""]);
        .Call $arguments.Add(
            ""input"",
            (System.Object)$input)
    }
}", result.Source);
            Assert.Equal(2, result.Parameters.Count);
            Assert.Equal(typeof(int), result.Parameters["targetid"]);
            Assert.Equal(typeof(int), result.Parameters["id"]);

            IDictionary<string, object> arguments = new Dictionary<string, object> { { "targetid", 9 } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(arguments, dependencyResolver.Object);

            Assert.Equal(3, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.Equal(9, arguments["targetid"]);
            Assert.IsType<ExplicitHttpParameterInput>(arguments["input"]);
            Assert.Equal(9, ((ExplicitHttpParameterInput)arguments["input"]).targetid);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_UriSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpParameterInput input, int id) { }
    }
}
