using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource
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

        private static void Compile_Default_Target(IDatabaseAccessorFactory databaseAccessorFactory, out int x) => x = default;

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
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""lcid""] = (System.Object)$localeSource.LocaleId
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
            IHttpParameterResolutionMethod result = Compile(x => x.ResolveParameterFromSource("applicationid", "APPLICATION", "ApplicationId"));
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Tests.HttpParameterResolverTest+ApplicationHttpParameterSource $applicationSource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $applicationSource = .New Dibix.Tests.HttpParameterResolverTest+ApplicationHttpParameterSource();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""applicationid""] = (System.Object).Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""applicationid"",
            $applicationSource.ApplicationId)
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
Parameter: applicationid", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("Null object cannot be converted to a value type.", exception.InnerException.Message);
        }
        private static void Compile_PropertySource_WithInvalidCast_Throws_Target(IDatabaseAccessorFactory databaseAccessorFactory, byte applicationid) { }

        [Fact]
        public void Compile_PropertySource_WithUnknownSource_Throws()
        {
            Exception exception = Assert.Throws<InvalidOperationException>(() => Compile(x => x.ResolveParameterFromSource("lcid", "UNKNOWNSOURCE", "LocaleId")));
            Assert.Equal(@"Http parameter resolver compilation failed
at GET Dibix/Test
Parameter: lcid", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("No source with the name 'UNKNOWNSOURCE' is registered", exception.InnerException.Message);
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
                x.ResolveParameterFromSource("agentid", "BODY", "Detail.AgentId");
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
        Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $bodySource = .Call Dibix.Http.HttpParameterResolverUtility.ReadBody($arguments);
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""lcid""] = (System.Object)$bodySource.LocaleId;
        $arguments.Item[""agentid""] = (System.Object)($bodySource.Detail).AgentId;
        $arguments.Item[""itemsa_""] = (System.Object).Call Dibix.StructuredType`1[Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyItemSet].From(
            $bodySource.ItemsA,
            .Lambda #Lambda2<System.Action`3[Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyItemSet,Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyItem,System.Int32]>)
        ;
        $input = .New Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyParameterInput();
        $input.targetid = .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""targetid"",
            $bodySource.SourceId);
        $arguments.Item[""input""] = (System.Object)$input
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
            Assert.Equal(HttpParameterLocation.NonUser, result.Parameters["$body"].Location);
            Assert.False(result.Parameters["$body"].IsOptional);

            object body = new ExplicitHttpBody
            {
                SourceId = 7,
                LocaleId = 1033,
                Detail = new ExplicitHttpBodyDetail { AgentId = 710 },
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

            Assert.Equal(6, arguments.Count);
            Assert.Equal(body, arguments["$body"]);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            ExplicitHttpBodyParameterInput input = Assert.IsType<ExplicitHttpBodyParameterInput>(arguments["input"]);
            Assert.Equal(7, input.targetid);
            Assert.Equal(1033, arguments["lcid"]);
            Assert.Equal(710, arguments["agentid"]);
            ExplicitHttpBodyItemSet itemsa_ = Assert.IsType<ExplicitHttpBodyItemSet>(arguments["itemsa_"]);
            Assert.Equal(@"id_ INT(4)  idx INT(4)  age_ INT(4)  name_ NVARCHAR(MAX)
----------  ----------  -----------  -------------------
1033        1           5            X                  
1033        2           5            Y                  ", itemsa_.Dump());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_ExplicitBodySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpBodyParameterInput input, int lcid, int agentid, ExplicitHttpBodyItemSet itemsa_) { }

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
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""userid""] = (System.Object)$bodySource.UserId;
        $arguments.Item[""itemsa""] = (System.Object).Call Dibix.StructuredType`1[Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet].From(
            $bodySource.ItemsA,
            .Lambda #Lambda2<System.Action`3[Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet,Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItem,System.Int32]>)
        ;
        $input = .New Dibix.Tests.HttpParameterResolverTest+ImplicitBodyHttpParameterInput();
        $input.sourceid = .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""sourceid"",
            $bodySource.SourceId);
        $input.localeid = $bodySource.LocaleId;
        $input.fromuri = .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""fromuri"",
            $arguments.Item[""fromuri""]);
        $arguments.Item[""input""] = (System.Object)$input
    }
}

.Lambda #Lambda2<System.Action`3[Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet,Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItem,System.Int32]>(
    Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet $x,
    Dibix.Tests.HttpParameterResolverTest+ImplicitHttpBodyItem $y,
    System.Int32 $i) {
    .Call $x.Add(
        .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""type"",
            $y.Type),
        $y.Name)
}", result.Source);
            Assert.Equal(3, result.Parameters.Count);
            Assert.Equal("$body", result.Parameters["$body"].Name);
            Assert.Equal(typeof(ImplicitHttpBody), result.Parameters["$body"].Type);
            Assert.Equal(HttpParameterLocation.NonUser, result.Parameters["$body"].Location);
            Assert.False(result.Parameters["$body"].IsOptional);
            Assert.Equal("id", result.Parameters["id"].Name);
            Assert.Equal(typeof(int), result.Parameters["id"].Type);
            Assert.Equal(HttpParameterLocation.Query, result.Parameters["id"].Location);
            Assert.False(result.Parameters["id"].IsOptional);
            Assert.Equal("fromuri", result.Parameters["fromuri"].Name);
            Assert.Equal(typeof(int), result.Parameters["fromuri"].Type);
            Assert.Equal(HttpParameterLocation.Query, result.Parameters["fromuri"].Location);
            Assert.False(result.Parameters["fromuri"].IsOptional);

            object body = new ImplicitHttpBody
            {
                SourceId = 7,
                LocaleId = 1033,
                UserId = 5,
                ItemsA =
                {
                    new ImplicitHttpBodyItem(ImplicitHttpBodyItemType.A, "X"),
                    new ImplicitHttpBodyItem(ImplicitHttpBodyItemType.B, "Y")
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
            ImplicitBodyHttpParameterInput input = Assert.IsType<ImplicitBodyHttpParameterInput>(arguments["input"]);
            Assert.Equal(7, input.sourceid);
            Assert.Equal(1033, input.localeid);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
            ImplicitHttpBodyItemSet itemsa = Assert.IsType<ImplicitHttpBodyItemSet>(arguments["itemsa"]);
            Assert.Equal(@"type SMALLINT(2)  name NVARCHAR(MAX)
----------------  ------------------
1                 X                 
2                 Y                 ", itemsa.Dump());
        }
        private static void Compile_ImplicitBodySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, int id, [InputClass] ImplicitBodyHttpParameterInput input, int userid, ImplicitHttpBodyItemSet itemsa) { }

        [Fact]
        public void Compile_BodySource_WithConverter()
        {
            HttpParameterConverterRegistry.Register<EncryptionHttpParameterConverter>("CRYPT1");
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.BodyContract = typeof(HttpBody);
                x.ResolveParameterFromSource("encryptedpassword", "BODY", "Password", "CRYPT1");
                x.ResolveParameterFromSource("anotherencryptedpassword", "BODY", "Detail.Password", "CRYPT1");
                x.ResolveParameterFromSource("items", "BODY", "Items", y =>
                {
                    y.ResolveParameterFromSource("encryptedpassword", "ITEM", "Password", "CRYPT1");
                });
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Tests.HttpParameterResolverTest+HttpBody $bodySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $bodySource = .Call Dibix.Http.HttpParameterResolverUtility.ReadBody($arguments);
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""encryptedpassword""] = (System.Object).Call Dibix.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert($bodySource.Password)
        ;
        $arguments.Item[""anotherencryptedpassword""] = (System.Object).Call Dibix.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert(($bodySource.Detail).Password)
        ;
        $arguments.Item[""items""] = (System.Object).Call Dibix.StructuredType`1[Dibix.Tests.HttpParameterResolverTest+HttpBodyItemSet].From(
            $bodySource.Items,
            .Lambda #Lambda2<System.Action`3[Dibix.Tests.HttpParameterResolverTest+HttpBodyItemSet,Dibix.Tests.HttpParameterResolverTest+HttpBodyItem,System.Int32]>)
    }
}

.Lambda #Lambda2<System.Action`3[Dibix.Tests.HttpParameterResolverTest+HttpBodyItemSet,Dibix.Tests.HttpParameterResolverTest+HttpBodyItem,System.Int32]>(
    Dibix.Tests.HttpParameterResolverTest+HttpBodyItemSet $x,
    Dibix.Tests.HttpParameterResolverTest+HttpBodyItem $y,
    System.Int32 $i) {
    .Call $x.Add(.Call Dibix.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert($y.Password))
}", result.Source);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal("$body", result.Parameters["$body"].Name);
            Assert.Equal(typeof(HttpBody), result.Parameters["$body"].Type);
            Assert.Equal(HttpParameterLocation.NonUser, result.Parameters["$body"].Location);
            Assert.False(result.Parameters["$body"].IsOptional);

            object body = new HttpBody
            {
                Password = "Cake",
                Detail = new HttpBodyDetail { Password = "Cookie" },
                Items =
                {
                    new HttpBodyItem("Item1")
                  , new HttpBodyItem("Item2")
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
            Assert.Equal("ENCRYPTED(Cake)", arguments["encryptedpassword"]);
            Assert.Equal("ENCRYPTED(Cookie)", arguments["anotherencryptedpassword"]);
            HttpBodyItemSet items = Assert.IsType<HttpBodyItemSet>(arguments["items"]);
            Assert.Equal(@"encryptedpassword NVARCHAR(MAX)
-------------------------------
ENCRYPTED(Item1)               
ENCRYPTED(Item2)               ", items.Dump());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_BodySource_WithConverter_Target(IDatabaseAccessorFactory databaseAccessorFactory, string encryptedpassword, string anotherencryptedpassword, HttpBodyItemSet items) { }
        
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
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        .Call Dibix.Http.HttpParameterResolver.AddParameterFromBody(
            $arguments,
            ""value"");
        $input = .New Dibix.Tests.HttpParameterResolverTest+XmlHttpParameterInput();
        $input.data = .Call Dibix.Http.HttpParameterResolver.ConvertParameterFromBody($arguments);
        $arguments.Item[""input""] = (System.Object)$input
    }
}", result.Source);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal("$body", result.Parameters["$body"].Name);
            Assert.Equal(typeof(JObject), result.Parameters["$body"].Type);
            Assert.Equal(HttpParameterLocation.NonUser, result.Parameters["$body"].Location);
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
            XmlHttpParameterInput input = Assert.IsType<XmlHttpParameterInput>(arguments["input"]);
            Assert.Equal("<id>5</id>", input.data.ToString());
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
        Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $input = .New Dibix.Tests.HttpParameterResolverTest+ExplicitHttpBodyParameterInput();
        .Call Dibix.Http.HttpParameterResolver.BindParametersFromBody(
            $arguments,
            $input);
        $arguments.Item[""input""] = (System.Object)$input
    }
}", result.Source);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal("$body", result.Parameters["$body"].Name);
            Assert.Equal(typeof(ExplicitHttpBody), result.Parameters["$body"].Type);
            Assert.Equal(HttpParameterLocation.NonUser, result.Parameters["$body"].Location);
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
            ExplicitHttpBodyParameterInput input = Assert.IsType<ExplicitHttpBodyParameterInput>(arguments["input"]);
            Assert.Equal(7, input.targetid);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_BodyBinder_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpBodyParameterInput input) { }

        [Fact]
        public void Compile_BodyBinder_WithoutInputClass_Throws()
        {
            Exception exception = Assert.Throws<InvalidOperationException>(() => Compile(x =>
            {
                x.BodyContract = typeof(ExplicitHttpBody);
                x.BodyBinder = typeof(FormattedInputBinder);
            }));
            Assert.Equal(@"Http parameter resolver compilation failed
at GET Dibix/Test
Parameter: input", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.Equal("Using a binder for the body is only supported if the target parameter is a class and is marked with the Dibix.InputClassAttribute", exception.InnerException.Message);
        }
        private static void Compile_BodyBinder_WithoutInputClass_Throws_Target(IDatabaseAccessorFactory databaseAccessorFactory, ExplicitHttpBodyParameterInput input) { }

        [Fact]
        public void Compile_ConstantSource()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.ResolveParameterFromConstant("boolValue", true);
                x.ResolveParameterFromConstant("intValue", 2);
                x.ResolveParameterFromNull("nullValue");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""boolValue""] = (System.Object)True;
        $arguments.Item[""intValue""] = (System.Object)2;
        $arguments.Item[""nullValue""] = (System.Object)null
    }
}", result.Source);
            Assert.False(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.Equal(4, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.Equal(true, arguments["boolValue"]);
            Assert.Equal(2, arguments["intValue"]);
            Assert.Equal(null, arguments["nullValue"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_ConstantSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, bool boolValue, int intValue, Guid? nullValue) { }

        [Fact]
        public void Compile_QuerySource()
        {
            HttpParameterConverterRegistry.Register<EncryptionHttpParameterConverter>("CRYPT2");
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.ChildRoute = "{cake}/{fart}";
                x.ResolveParameterFromSource("true", "QUERY", "true_");
                x.ResolveParameterFromSource("name", "QUERY", "name_", "CRYPT2");
                x.ResolveParameterFromSource("targetname", "QUERY", "targetname_", "CRYPT2");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        System.Object $idDefaultValue,
        System.Object $nameDefaultValue,
        System.Object $trueDefaultValue,
        Dibix.Tests.HttpParameterResolverTest+ExplicitHttpUriParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        .If (
            .Call $arguments.TryGetValue(
                ""id"",
                $idDefaultValue) & $idDefaultValue == null
        ) {
            $arguments.Item[""id""] = (System.Object)0
        } .Else {
            .Default(System.Void)
        };
        $arguments.Item[""name""] = (System.Object)$arguments.Item[""name_""];
        .If (
            .Call $arguments.TryGetValue(
                ""name"",
                $nameDefaultValue) & $nameDefaultValue == null
        ) {
            $arguments.Item[""name""] = (System.Object)""Cake""
        } .Else {
            .Default(System.Void)
        };
        $arguments.Item[""name""] = (System.Object).Call Dibix.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert(.Call Dibix.Http.HttpParameterResolver.ConvertValue(
                ""name"",
                $arguments.Item[""name""]));
        $arguments.Item[""true""] = (System.Object)$arguments.Item[""true_""];
        .If (
            .Call $arguments.TryGetValue(
                ""true"",
                $trueDefaultValue) & $trueDefaultValue == null
        ) {
            $arguments.Item[""true""] = (System.Object)True
        } .Else {
            .Default(System.Void)
        };
        $input = .New Dibix.Tests.HttpParameterResolverTest+ExplicitHttpUriParameterInput();
        $input.targetid = .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""targetid"",
            $arguments.Item[""targetid""]);
        $input.targetname = .Call Dibix.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert(.Call Dibix.Http.HttpParameterResolver.ConvertValue(
                ""targetname"",
                $arguments.Item[""targetname_""]));
        $arguments.Item[""input""] = (System.Object)$input
    }
}", result.Source);
            Assert.Equal(8, result.Parameters.Count);
            Assert.Equal("targetid", result.Parameters["targetid"].Name);
            Assert.Equal(typeof(int), result.Parameters["targetid"].Type);
            Assert.Equal(HttpParameterLocation.Query, result.Parameters["targetid"].Location);
            Assert.False(result.Parameters["targetid"].IsOptional);
            Assert.Equal("items", result.Parameters["items"].Name);
            Assert.Equal(typeof(UriItemSet), result.Parameters["items"].Type);
            Assert.Equal(HttpParameterLocation.Query, result.Parameters["items"].Location);
            Assert.False(result.Parameters["items"].IsOptional);
            Assert.Equal("targetname_", result.Parameters["targetname_"].Name);
            Assert.Equal(typeof(string), result.Parameters["targetname_"].Type);
            Assert.Equal(HttpParameterLocation.Query, result.Parameters["targetname_"].Location);
            Assert.False(result.Parameters["targetname_"].IsOptional);
            Assert.Equal("id", result.Parameters["id"].Name);
            Assert.Equal(typeof(int), result.Parameters["id"].Type);
            Assert.Equal(HttpParameterLocation.Query, result.Parameters["id"].Location);
            Assert.True(result.Parameters["id"].IsOptional);
            Assert.Equal("anotherid", result.Parameters["anotherid"].Name);
            Assert.Equal(typeof(int), result.Parameters["anotherid"].Type);
            Assert.Equal(HttpParameterLocation.Query, result.Parameters["anotherid"].Location);
            Assert.False(result.Parameters["anotherid"].IsOptional);
            Assert.Equal("name_", result.Parameters["name_"].Name);
            Assert.Equal(typeof(string), result.Parameters["name_"].Type);
            Assert.Equal(HttpParameterLocation.Query, result.Parameters["name_"].Location);
            Assert.True(result.Parameters["name_"].IsOptional);
            Assert.Equal("true_", result.Parameters["true_"].Name);
            Assert.Equal(typeof(bool?), result.Parameters["true_"].Type);
            Assert.Equal(HttpParameterLocation.Query, result.Parameters["true_"].Location);
            Assert.True(result.Parameters["true_"].IsOptional);
            Assert.Equal("empty", result.Parameters["empty"].Name);
            Assert.Equal(typeof(bool?), result.Parameters["empty"].Type);
            Assert.Equal(HttpParameterLocation.Query, result.Parameters["empty"].Location);
            Assert.True(result.Parameters["empty"].IsOptional);

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "targetid", 9 }
              , { "targetname_", "Muffin" }
              , { "id", 10 }
              , { "anotherid", 5 }
              , { "name_", null } // Optional => Use default value
              , { "true_", null } // Optional => Use default value
              , { "empty", null } // Optional => Use default value
            };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.Equal(11, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.Equal(9, arguments["targetid"]);
            Assert.Equal("Muffin", arguments["targetname_"]);
            Assert.Equal(10, arguments["id"]);
            Assert.Equal(5, arguments["anotherid"]);
            Assert.Equal(null, arguments["name_"]);
            Assert.Equal("ENCRYPTED(Cake)", arguments["name"]);
            Assert.Equal(null, arguments["true_"]);
            Assert.Equal(true, arguments["true"]);
            Assert.Equal(null, arguments["empty"]);
            ExplicitHttpUriParameterInput input = Assert.IsType<ExplicitHttpUriParameterInput>(arguments["input"]);
            Assert.Equal(9, input.targetid);
            Assert.Equal("ENCRYPTED(Muffin)", input.targetname);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_QuerySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpUriParameterInput input, UriItemSet items, int anotherid, int id = 0, string name = "Cake", bool? @true = true, bool? empty = null) { }

        [Fact]
        public void Compile_PathSource()
        {
            HttpParameterConverterRegistry.Register<EncryptionHttpParameterConverter>("CRYPT3");
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.ChildRoute = "{targetid}/{targetname_}/{anotherid}";
                x.ResolveParameterFromSource("targetname", "PATH", "targetname_", "CRYPT3");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Tests.HttpParameterResolverTest+ExplicitHttpUriParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $input = .New Dibix.Tests.HttpParameterResolverTest+ExplicitHttpUriParameterInput();
        $input.targetid = .Call Dibix.Http.HttpParameterResolver.ConvertValue(
            ""targetid"",
            $arguments.Item[""targetid""]);
        $input.targetname = .Call Dibix.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert(.Call Dibix.Http.HttpParameterResolver.ConvertValue(
                ""targetname"",
                $arguments.Item[""targetname_""]));
        $arguments.Item[""input""] = (System.Object)$input
    }
}", result.Source);
            Assert.Equal(3, result.Parameters.Count);
            Assert.Equal("targetid", result.Parameters["targetid"].Name);
            Assert.Equal(typeof(int), result.Parameters["targetid"].Type);
            Assert.Equal(HttpParameterLocation.Path, result.Parameters["targetid"].Location);
            Assert.False(result.Parameters["targetid"].IsOptional);
            Assert.Equal("targetname_", result.Parameters["targetname_"].Name);
            Assert.Equal(typeof(string), result.Parameters["targetname_"].Type);
            Assert.Equal(HttpParameterLocation.Path, result.Parameters["targetname_"].Location);
            Assert.False(result.Parameters["targetname_"].IsOptional);
            Assert.Equal("anotherid", result.Parameters["anotherid"].Name);
            Assert.Equal(typeof(int), result.Parameters["anotherid"].Type);
            Assert.Equal(HttpParameterLocation.Path, result.Parameters["anotherid"].Location);
            Assert.False(result.Parameters["anotherid"].IsOptional);

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "targetid", 9 }
              , { "targetname_", "Muffin" }
              , { "anotherid", 5 }
            };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.Equal(5, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.Equal(9, arguments["targetid"]);
            Assert.Equal("Muffin", arguments["targetname_"]);
            ExplicitHttpUriParameterInput input = Assert.IsType<ExplicitHttpUriParameterInput>(arguments["input"]);
            Assert.Equal(9, input.targetid);
            Assert.Equal("ENCRYPTED(Muffin)", input.targetname);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_PathSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpUriParameterInput input, int anotherid) { }

        [Fact]
        public void Compile_RequestSource()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.ResolveParameterFromSource("regionlanguage", "REQUEST", "Language");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""regionlanguage""] = (System.Object).Call Dibix.Http.RequestParameterSourceProvider.GetLanguage($request)
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
            Assert.Equal("en-US", arguments["regionlanguage"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_RequestSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, string regionlanguage) { }

        [Fact]
        public void Compile_EnvironmentSource()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.ResolveParameterFromSource("machinename", "ENV", "MachineName");
                x.ResolveParameterFromSource("pid", "ENV", "CurrentProcessId");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.IParameterDependencyResolver $dependencyResolver) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""machinename""] = (System.Object).Call Dibix.Http.EnvironmentParameterSourceProvider.GetMachineName();
        $arguments.Item[""pid""] = (System.Object).Call Dibix.Http.EnvironmentParameterSourceProvider.GetCurrentProcessId()
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
            Assert.Equal(Dns.GetHostEntry(String.Empty).HostName, arguments["machinename"]);
            Assert.Equal(Process.GetCurrentProcess().Id, arguments["pid"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_EnvironmentSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, string machinename, int pid) { }
    }
}
