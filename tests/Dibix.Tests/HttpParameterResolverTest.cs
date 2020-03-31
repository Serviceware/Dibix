using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
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
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactorySource)
    }
}", result.Source);
            Assert.False(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.Equal(1, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_Default_Target(IDatabaseAccessorFactory databaseAccessorFactory) { }

        [Fact]
        public void Compile_PropertySource()
        {
            HttpParameterSourceProviderRegistry.Register<LocaleParameterHttpSourceProvider>("LOCALE");
            IHttpParameterResolutionMethod result = Compile(x => x.ResolveParameterFromSource("lcid", "LOCALE", "LocaleId"));
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Tests.HttpParameterResolverTest+LocaleHttpParameterSource $localeSource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $localeSource = .New Dibix.Tests.HttpParameterResolverTest+LocaleHttpParameterSource();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactorySource);
        .Call $arguments.Add(
            ""lcid"",
            (System.Object)$localeSource.LocaleId)
    }
}", result.Source);
            Assert.False(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

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
            IHttpParameterResolutionMethod result = Compile(x => x.ResolveParameterFromSource("applicationId", "APPLICATION", "ApplicationId"));
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Tests.HttpParameterResolverTest+ApplicationHttpParameterSource $applicationSource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $applicationSource = .New Dibix.Tests.HttpParameterResolverTest+ApplicationHttpParameterSource();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactorySource);
        .Call $arguments.Add(
            ""applicationId"",
            (System.Object).Call Dibix.Http.HttpParameterResolver.ConvertValue(
                ""applicationId"",
                $applicationSource.ApplicationId))
    }
}", result.Source);
            Assert.False(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            Exception exception = Assert.Throws<InvalidOperationException>(() => result.PrepareParameters(request, arguments, dependencyResolver.Object));
            Assert.Equal(@"Parameter mapping failed
Parameter: applicationId", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("Null object cannot be converted to a value type.", exception.InnerException.Message);
        }
        private static void Compile_PropertySource_WithInvalidCast_Throws_Target(IDatabaseAccessorFactory databaseAccessorFactory, byte applicationId) { }

        [Fact]
        public void Compile_PropertySource_WithUnknownSource_Throws()
        {
            Exception exception = Assert.Throws<InvalidOperationException>(() => Compile(x => x.ResolveParameterFromSource("lcid", "UNKNOWNSOURCE", "LocaleId")));
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
                x.ResolveParameterFromSource("targetid", "BODY", "SourceId");
                x.ResolveParameterFromSource("lcid", "BODY", "LocaleId");
                x.ResolveParameterFromSource("itemsa_", "BODY", "ItemsA", y =>
                {
                    y.ResolveParameterFromSource("id_", "BODY", "LocaleId");
                    y.ResolveParameterFromSource("idx", "ITEM", "$INDEX");
                    y.ResolveParameterFromConstant("age_", 5);
                    y.ResolveParameterFromSource("name_", "ITEM", "Name");
                });
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBody $bodySource,
        Dibix.Tests.HttpParameterResolverTest+ExplicitHttpParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $bodySource = .Call Dibix.Http.HttpParameterResolverUtility.ReadBody($arguments);
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactorySource);
        .Call $arguments.Add(
            ""lcid"",
            (System.Object)$bodySource.LocaleId);
        .Call $arguments.Add(
            ""itemsa_"",
            (System.Object).Call Dibix.StructuredType`1[Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyItemSet].From(
                $bodySource.ItemsA,
                .Lambda #Lambda2<System.Action`3[Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyItemSet,Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyItem,System.Int32]>)
        );
        $input = .New Dibix.Tests.HttpParameterResolverTest+ExplicitHttpParameterInput();
        $input.targetid = .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""targetid"",
            $bodySource.SourceId);
        .Call $arguments.Add(
            ""input"",
            (System.Object)$input)
    }
}

.Lambda #Lambda2<System.Action`3[Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyItemSet,Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyItem,System.Int32]>(
    Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyItemSet $x,
    Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyItem $y,
    System.Int32 $i) {
    .Call $x.Add(
        $bodySource.LocaleId,
        $i,
        5,
        $y.Name)
}", result.Source);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal("$body", result.Parameters["$body"].Name);
            Assert.Equal(typeof(ExplicitHttpBody), result.Parameters["$body"].Type);
            Assert.False(result.Parameters["$body"].IsOptional);

            object body = new ExplicitHttpBody
            {
                SourceId = 7,
                LocaleId = 1033,
                ItemsA =
                {
                    new ExplicitHttpBodyItem(1, "X"),
                    new ExplicitHttpBodyItem(2, "Y")
                }
            };
            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "$body", body } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.Equal(5, arguments.Count);
            Assert.Equal(body, arguments["$body"]);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.IsType<ExplicitHttpParameterInput>(arguments["input"]);
            Assert.Equal(7, ((ExplicitHttpParameterInput)arguments["input"]).targetid);
            Assert.Equal(1033, arguments["lcid"]);
            Assert.IsType<ExplicitHttpBodyItemSet>(arguments["itemsa_"]);
            Assert.Equal(@"id_ INT(4)  idx INT(4)  age_ INT(4)  name_ NVARCHAR(MAX)
----------  ----------  -----------  -------------------
1033        1           5            X                  
1033        2           5            Y                  ", ((ExplicitHttpBodyItemSet)arguments["itemsa_"]).Dump());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_ExplicitBodySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpParameterInput input, int lcid, ExplicitHttpBodyItemSet itemsa_) { }

        [Fact]
        public void Compile_ImplicitBodySource()
        {
            IHttpParameterResolutionMethod result = Compile(x => x.BodyContract = typeof(ImplicitHttpBody));
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBody $bodySource,
        Dibix.Tests.HttpParameterResolverTest+ImplicitBodyHttpParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $bodySource = .Call Dibix.Http.HttpParameterResolverUtility.ReadBody($arguments);
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactorySource);
        .Call $arguments.Add(
            ""userid"",
            (System.Object)$bodySource.UserId);
        .Call $arguments.Add(
            ""itemsa"",
            (System.Object).Call Dibix.StructuredType`1[Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet].From(
                $bodySource.ItemsA,
                .Lambda #Lambda2<System.Action`3[Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet,Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItem,System.Int32]>)
        );
        $input = .New Dibix.Tests.HttpParameterResolverTest+ImplicitBodyHttpParameterInput();
        $input.sourceid = .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""sourceid"",
            $bodySource.SourceId);
        $input.localeid = $bodySource.LocaleId;
        $input.fromuri = .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""fromuri"",
            $arguments.Item[""fromuri""]);
        .Call $arguments.Add(
            ""input"",
            (System.Object)$input)
    }
}

.Lambda #Lambda2<System.Action`3[Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet,Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItem,System.Int32]>(
    Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet $x,
    Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItem $y,
    System.Int32 $i) {
    .Call $x.Add(
        $y.Id,
        $y.Name)
}", result.Source);
            Assert.Equal(3, result.Parameters.Count);
            Assert.Equal("$body", result.Parameters["$body"].Name);
            Assert.Equal(typeof(ImplicitHttpBody), result.Parameters["$body"].Type);
            Assert.False(result.Parameters["$body"].IsOptional);
            Assert.Equal("id", result.Parameters["id"].Name);
            Assert.Equal(typeof(int), result.Parameters["id"].Type);
            Assert.False(result.Parameters["id"].IsOptional);
            Assert.Equal("fromuri", result.Parameters["fromuri"].Name);
            Assert.Equal(typeof(int), result.Parameters["fromuri"].Type);
            Assert.False(result.Parameters["fromuri"].IsOptional);

            object body = new ImplicitHttpBody
            {
                SourceId = 7,
                LocaleId = 1033,
                UserId = 5,
                ItemsA =
                {
                    new ImplicitHttpBodyItem(1, "X"),
                    new ImplicitHttpBodyItem(2, "Y")
                }
            };
            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "$body", body }
              , { "id", 2 }
              , { "fromuri", 3 }
            };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.Equal(7, arguments.Count);
            Assert.Equal(2, arguments["id"]);
            Assert.Equal(5, arguments["userid"]);
            Assert.Equal(3, arguments["fromuri"]);
            Assert.Equal(body, arguments["$body"]);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.IsType<ImplicitBodyHttpParameterInput>(arguments["input"]);
            Assert.Equal(7, ((ImplicitBodyHttpParameterInput)arguments["input"]).sourceid);
            Assert.Equal(1033, ((ImplicitBodyHttpParameterInput)arguments["input"]).localeid);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
            Assert.IsType<ImplicitHttpBodyItemSet>(arguments["itemsa"]);
            Assert.Equal(@"id INT(4)  name NVARCHAR(MAX)
---------  ------------------
1          X                 
2          Y                 ", ((ImplicitHttpBodyItemSet)arguments["itemsa"]).Dump());
        }
        private static void Compile_ImplicitBodySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, int id, [InputClass] ImplicitBodyHttpParameterInput input, int userid, ImplicitHttpBodyItemSet itemsa) { }

        [Fact]
        public void Compile_BodyConverter()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.BodyContract = typeof(JObject);
                x.ResolveParameterFromBody("data", typeof(JsonToXmlConverter).AssemblyQualifiedName);
                x.ResolveParameterFromBody("value", typeof(JsonToXmlConverter).AssemblyQualifiedName);
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Tests.HttpParameterResolverTest+XmlHttpParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactorySource);
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
            Assert.Equal("$body", result.Parameters["$body"].Name);
            Assert.Equal(typeof(JObject), result.Parameters["$body"].Type);
            Assert.False(result.Parameters["$body"].IsOptional);

            object body = JObject.Parse("{\"id\":5}");
            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "$body", body } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

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
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Tests.HttpParameterResolverTest+ExplicitHttpParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactorySource);
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
            Assert.Equal("$body", result.Parameters["$body"].Name);
            Assert.Equal(typeof(ExplicitHttpBody), result.Parameters["$body"].Type);
            Assert.False(result.Parameters["$body"].IsOptional);

            object body = new ExplicitHttpBody { SourceId = 7 };
            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "$body", body } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

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
            IHttpParameterResolutionMethod result = Compile(x => x.ResolveParameterFromConstant("value", true));
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactorySource);
        .Call $arguments.Add(
            ""value"",
            (System.Object)True)
    }
}", result.Source);
            Assert.False(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

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
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Tests.HttpParameterResolverTest+ExplicitHttpParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactorySource);
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
            Assert.Equal("targetid", result.Parameters["targetid"].Name);
            Assert.Equal(typeof(int), result.Parameters["targetid"].Type);
            Assert.False(result.Parameters["targetid"].IsOptional);
            Assert.Equal("id", result.Parameters["id"].Name);
            Assert.Equal(typeof(int), result.Parameters["id"].Type);
            Assert.True(result.Parameters["id"].IsOptional);

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "targetid", 9 } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.Equal(3, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.Equal(9, arguments["targetid"]);
            Assert.IsType<ExplicitHttpParameterInput>(arguments["input"]);
            Assert.Equal(9, ((ExplicitHttpParameterInput)arguments["input"]).targetid);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_UriSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpParameterInput input, int id = 0) { }

        [Fact]
        public void Compile_RequestSource()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.ResolveParameterFromSource("regionLanguage", "REQUEST", "Language");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Http.RequestParameterSource $requestSource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $requestSource = .New Dibix.Http.RequestParameterSource($request);
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactorySource);
        .Call $arguments.Add(
            ""regionLanguage"",
            (System.Object)$requestSource.Language)
    }
}", result.Source);
            Assert.False(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.Equal(2, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.Equal("en-US", arguments["regionLanguage"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_RequestSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, string regionLanguage) { }

        [Fact]
        public void Compile_EnvironmentSource()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.ResolveParameterFromSource("machineName", "ENV", "MachineName");
                x.ResolveParameterFromSource("pid", "ENV", "CurrentProcessId");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Http.EnvironmentParameterSource $envSource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $envSource = .New Dibix.Http.EnvironmentParameterSource();
        .Call $arguments.Add(
            ""databaseAccessorFactory"",
            (System.Object)$databaseaccessorfactorySource);
        .Call $arguments.Add(
            ""machineName"",
            (System.Object)$envSource.MachineName);
        .Call $arguments.Add(
            ""pid"",
            (System.Object)$envSource.CurrentProcessId)
    }
}", result.Source);
            Assert.False(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.Equal(3, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.Equal(Environment.MachineName, arguments["machineName"]);
            Assert.Equal(Process.GetCurrentProcess().Id, arguments["pid"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_EnvironmentSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, string machineName, int pid) { }
    }
}
