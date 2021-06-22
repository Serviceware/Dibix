using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;
using Dibix.Tests;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Dibix.Http.Server.Tests
{
    public partial class HttpParameterResolverTest
    {
        [Fact]
        public void Compile_Default()
        {
            IHttpParameterResolutionMethod result = Compile();
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
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
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.ResolveParameterFromSource("lcid", "LOCALE", "LocaleId");
                x.ResolveParameterFromSource("locale", "LOCALE", "$SELF");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+LocaleHttpParameterSource $localeSource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $localeSource = .New Dibix.Http.Server.Tests.HttpParameterResolverTest+LocaleHttpParameterSource();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""lcid""] = (System.Object)$localeSource.LocaleId;
        $arguments.Item[""locale""] = (System.Object)$localeSource
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
            Assert.Equal(1033, arguments["lcid"]);
            LocaleHttpParameterSource locale = Assert.IsType<LocaleHttpParameterSource>(arguments["locale"]);
            Assert.Equal(1033, locale.LocaleId);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_PropertySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, int lcid, LocaleHttpParameterSource locale) { }

        [Fact]
        public void Compile_PropertySource_WithInvalidCast_Throws()
        {
            HttpParameterSourceProviderRegistry.Register<ApplicationHttpParameterSourceProvider>("APPLICATION");
            IHttpParameterResolutionMethod result = Compile(x => x.ResolveParameterFromSource("applicationid", "APPLICATION", "ApplicationId"));
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+ApplicationHttpParameterSource $applicationSource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $applicationSource = .New Dibix.Http.Server.Tests.HttpParameterResolverTest+ApplicationHttpParameterSource();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""applicationid""] = (System.Object).Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
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
                x.ResolveParameterFromSource("skip", "BODY", "OptionalDetail.Nested.Skip");
                x.ResolveParameterFromSource("itemsa_", "BODY", "ItemsA", y =>
                {
                    y.ResolveParameterFromSource("id_", "BODY", "Detail.AgentId");
                    y.ResolveParameterFromSource("idx", "ITEM", "$INDEX");
                    y.ResolveParameterFromConstant("age_", 5);
                    y.ResolveParameterFromSource("name_", "ITEM", "Name");
                });
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpBody $bodySource,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpBodyParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $bodySource = .Call Dibix.Http.Server.HttpParameterResolverUtility.ReadBody($arguments);
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""lcid""] = (System.Object)$bodySource.LocaleId;
        $arguments.Item[""agentid""] = (System.Object).If ($bodySource.Detail != null) {
            ($bodySource.Detail).AgentId
        } .Else {
            .Default(System.Int32)
        };
        $arguments.Item[""itemsa_""] = (System.Object).Call Dibix.StructuredType`1[Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpBodyItemSet].From(
            $bodySource.ItemsA,
            .Lambda #Lambda2<System.Action`3[Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpBodyItemSet,Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpBodyItem,System.Int32]>)
        ;
        $arguments.Item[""skip""] = (System.Object).If (
            $bodySource.OptionalDetail != null && ($bodySource.OptionalDetail).Nested != null
        ) {
            (($bodySource.OptionalDetail).Nested).Skip
        } .Else {
            5
        };
        $input = .New Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpBodyParameterInput();
        $input.targetid = .Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
            ""targetid"",
            $bodySource.SourceId);
        $arguments.Item[""input""] = (System.Object)$input
    }
}

.Lambda #Lambda2<System.Action`3[Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpBodyItemSet,Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpBodyItem,System.Int32]>(
    Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpBodyItemSet $x,
    Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpBodyItem $y,
    System.Int32 $i) {
    .Call $x.Add(
        .If ($bodySource.Detail != null) {
            ($bodySource.Detail).AgentId
        } .Else {
            .Default(System.Int32)
        },
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

            Assert.Equal(7, arguments.Count);
            Assert.Equal(body, arguments["$body"]);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            ExplicitHttpBodyParameterInput input = Assert.IsType<ExplicitHttpBodyParameterInput>(arguments["input"]);
            Assert.Equal(7, input.targetid);
            Assert.Equal(1033, arguments["lcid"]);
            Assert.Equal(710, arguments["agentid"]);
            StructuredType itemsa_ = Assert.IsType<ExplicitHttpBodyItemSet>(arguments["itemsa_"]);
            Assert.Equal(@"id_ INT(4)  idx INT(4)  age_ INT(4)  name_ NVARCHAR(MAX)
----------  ----------  -----------  -------------------
710         1           5            X                  
710         2           5            Y                  ", itemsa_.Dump());
            Assert.Equal(5, arguments["skip"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_ExplicitBodySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpBodyParameterInput input, int lcid, int agentid, ExplicitHttpBodyItemSet itemsa_, int skip = 5) { }

        [Fact]
        public void Compile_ImplicitBodySource()
        {
            IHttpParameterResolutionMethod result = Compile(x => x.BodyContract = typeof(ImplicitHttpBody));
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBody $bodySource,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitBodyHttpParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $bodySource = .Call Dibix.Http.Server.HttpParameterResolverUtility.ReadBody($arguments);
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""userid""] = (System.Object)$bodySource.UserId;
        $arguments.Item[""itemsa""] = (System.Object).Call Dibix.StructuredType`1[Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet].From(
            $bodySource.ItemsA,
            .Lambda #Lambda2<System.Action`3[Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet,Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItem,System.Int32]>)
        ;
        $input = .New Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitBodyHttpParameterInput();
        $input.sourceid = .Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
            ""sourceid"",
            $bodySource.SourceId);
        $input.localeid = $bodySource.LocaleId;
        $input.fromuri = .Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
            ""fromuri"",
            $arguments.Item[""fromuri""]);
        $arguments.Item[""input""] = (System.Object)$input
    }
}

.Lambda #Lambda2<System.Action`3[Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet,Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItem,System.Int32]>(
    Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet $x,
    Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItem $y,
    System.Int32 $i) {
    .Call $x.Add(
        .Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
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
            StructuredType itemsa = Assert.IsType<ImplicitHttpBodyItemSet>(arguments["itemsa"]);
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
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBody $bodySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $bodySource = .Call Dibix.Http.Server.HttpParameterResolverUtility.ReadBody($arguments);
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""encryptedpassword""] = (System.Object).Call Dibix.Http.Server.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert($bodySource.Password)
        ;
        $arguments.Item[""anotherencryptedpassword""] = (System.Object).Call Dibix.Http.Server.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert(.If ($bodySource.Detail !=
            null) {
                ($bodySource.Detail).Password
            } .Else {
                .Default(System.String)
            });
        $arguments.Item[""items""] = (System.Object).Call Dibix.StructuredType`1[Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItemSet].From(
            $bodySource.Items,
            .Lambda #Lambda2<System.Action`3[Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItemSet,Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItem,System.Int32]>)
    }
}

.Lambda #Lambda2<System.Action`3[Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItemSet,Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItem,System.Int32]>(
    Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItemSet $x,
    Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItem $y,
    System.Int32 $i) {
    .Call $x.Add(.Call Dibix.Http.Server.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert($y.Password)
    )
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
            StructuredType items = Assert.IsType<HttpBodyItemSet>(arguments["items"]);
            Assert.Equal(@"encryptedpassword NVARCHAR(MAX)
-------------------------------
ENCRYPTED(Item1)               
ENCRYPTED(Item2)               ", items.Dump());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_BodySource_WithConverter_Target(IDatabaseAccessorFactory databaseAccessorFactory, string encryptedpassword, string anotherencryptedpassword, HttpBodyItemSet items) { }

        [Fact]
        public void Compile_BodySource_Raw()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.BodyContract = typeof(Stream);
                x.ResolveParameterFromSource("data", "BODY", "$RAW");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""data""] = (System.Object).Call (.Call (.Call ($request.Content).ReadAsStreamAsync()).GetAwaiter()).GetResult()
    }
}", result.Source);
            Assert.False(result.Parameters.Any());

            byte[] data = { 1, 2 };
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new ByteArrayContent(data);
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.Equal(2, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            MemoryStream stream = Assert.IsType<MemoryStream>(arguments["data"]);
            Assert.Equal(data.AsEnumerable(), stream.ToArray().AsEnumerable());
        }
        private static void Compile_BodySource_Raw_Target(IDatabaseAccessorFactory databaseAccessorFactory, Stream data) { }
        
        [Fact]
        public void Compile_BodyConverter()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.BodyContract = typeof(JObject);
                x.ResolveParameterFromBody("data", typeof(JsonToXmlConverter).AssemblyQualifiedName);
                x.ResolveParameterFromBody("value", typeof(JsonToXmlConverter).AssemblyQualifiedName);
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+XmlHttpParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        .Call Dibix.Http.Server.HttpParameterResolver.AddParameterFromBody(
            $arguments,
            ""value"");
        $input = .New Dibix.Http.Server.Tests.HttpParameterResolverTest+XmlHttpParameterInput();
        $input.data = .Call Dibix.Http.Server.HttpParameterResolver.ConvertParameterFromBody($arguments);
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
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpBodyParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $input = .New Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpBodyParameterInput();
        .Call Dibix.Http.Server.HttpParameterResolver.BindParametersFromBody(
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
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
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
            Assert.Null(arguments["nullValue"]);
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
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        System.Object $idDefaultValue,
        System.Object $nameDefaultValue,
        System.Object $trueDefaultValue,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpUriParameterInput $input) {
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
        $arguments.Item[""name""] = (System.Object).Call Dibix.Http.Server.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert(.Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
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
        $input = .New Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpUriParameterInput();
        $input.targetid = .Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
            ""targetid"",
            $arguments.Item[""targetid""]);
        $input.targetname = .Call Dibix.Http.Server.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert(.Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
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
            Assert.Equal(typeof(StringSet), result.Parameters["items"].Type);
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
            Assert.Null(arguments["name_"]);
            Assert.Equal("ENCRYPTED(Cake)", arguments["name"]);
            Assert.Null(arguments["true_"]);
            Assert.Equal(true, arguments["true"]);
            Assert.Null(arguments["empty"]);
            ExplicitHttpUriParameterInput input = Assert.IsType<ExplicitHttpUriParameterInput>(arguments["input"]);
            Assert.Equal(9, input.targetid);
            Assert.Equal("ENCRYPTED(Muffin)", input.targetname);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_QuerySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpUriParameterInput input, StringSet items, int anotherid, int id = 0, string name = "Cake", bool? @true = true, bool? empty = null) { }

        [Fact]
        public void Compile_PathSource()
        {
            HttpParameterConverterRegistry.Register<EncryptionHttpParameterConverter>("CRYPT3");
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.ChildRoute = "{targetid}/{targetname_}/{anotherid}";
                x.ResolveParameterFromSource("targetname", "PATH", "targetname_", "CRYPT3");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpUriParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $input = .New Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpUriParameterInput();
        $input.targetid = .Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
            ""targetid"",
            $arguments.Item[""targetid""]);
        $input.targetname = .Call Dibix.Http.Server.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert(.Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
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
        public void Compile_HeaderSource()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.ResolveParameterFromSource("authorization", "HEADER", "Authorization");
                x.ResolveParameterFromSource("tenantid", "HEADER", "X-Tenant-Id");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""authorization""] = (System.Object).Call Dibix.Http.Server.HeaderParameterSourceProvider.GetHeader(
            $request,
            ""Authorization"");
        $arguments.Item[""tenantid""] = (System.Object).Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
            ""tenantid"",
            .Call Dibix.Http.Server.HeaderParameterSourceProvider.GetHeader(
                $request,
                ""X-Tenant-Id""))
    }
}", result.Source);
            Assert.Equal(2, result.Parameters.Count);
            Assert.Equal("authorization", result.Parameters["authorization"].Name);
            Assert.Equal(typeof(string), result.Parameters["authorization"].Type);
            Assert.Equal(HttpParameterLocation.Header, result.Parameters["authorization"].Location);
            Assert.True(result.Parameters["authorization"].IsOptional);
            Assert.Equal("tenantid", result.Parameters["tenantid"].Name);
            Assert.Equal(typeof(int), result.Parameters["tenantid"].Type);
            Assert.Equal(HttpParameterLocation.Header, result.Parameters["tenantid"].Location);
            Assert.True(result.Parameters["tenantid"].IsOptional);

            HttpRequestMessage request = new HttpRequestMessage();
            request.Headers.Add("Authorization", "Bearer token");
            request.Headers.Add("X-Tenant-Id", "2");
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.Equal(3, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            AuthenticationHeaderValue authorization = AuthenticationHeaderValue.Parse((string)arguments["authorization"]);
            Assert.Equal("Bearer", authorization.Scheme);
            Assert.Equal("token", authorization.Parameter);
            Assert.Equal(2, arguments["tenantid"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_HeaderSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, string authorization, int tenantid) { }

        [Fact]
        public void Compile_RequestSource()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.ResolveParameterFromSource("primaryclientlanguage", "REQUEST", "Language");
                x.ResolveParameterFromSource("clientlanguages", "REQUEST", "Languages");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""primaryclientlanguage""] = (System.Object).Call Dibix.Http.Server.RequestParameterSourceProvider.GetFirstLanguage($request)
        ;
        $arguments.Item[""clientlanguages""] = (System.Object).Block(
            Dibix.Http.Server.Tests.HttpParameterResolverTest+StringSet $clientlanguages,
            System.Collections.Generic.IEnumerator`1[System.String] $clientlanguagesEnumerator) {
            $clientlanguages = .New Dibix.Http.Server.Tests.HttpParameterResolverTest+StringSet();
            .Try {
                .Block() {
                    $clientlanguagesEnumerator = .Call (.Call Dibix.Http.Server.RequestParameterSourceProvider.GetLanguages($request)).GetEnumerator()
                    ;
                    .Loop  {
                        .If (
                            .IsTrue(.Call $clientlanguagesEnumerator.MoveNext())
                        ) {
                            .Block(System.String $clientlanguagesElement) {
                                $clientlanguagesElement = $clientlanguagesEnumerator.Current;
                                .Call $clientlanguages.Add($clientlanguagesElement)
                            }
                        } .Else {
                            .Break BreakClientlanguagesEnumerator { }
                        }
                    }
                    .LabelTarget BreakClientlanguagesEnumerator:
                }
            } .Finally {
                .If ($clientlanguagesEnumerator != null) {
                    .Call $clientlanguagesEnumerator.Dispose()
                } .Else {
                    .Default(System.Void)
                }
            };
            $clientlanguages
        }
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

            Assert.Equal(3, arguments.Count);
            Assert.Equal(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.Equal("en-US", arguments["primaryclientlanguage"]);
            StructuredType clientLanguages = Assert.IsType<StringSet>(arguments["clientlanguages"]);
            Assert.Equal(@"name NVARCHAR(MAX)
------------------
en-US             
en                ", clientLanguages.Dump());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_RequestSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, string primaryclientlanguage, StringSet clientlanguages) { }

        [Fact]
        public void Compile_EnvironmentSource()
        {
            IHttpParameterResolutionMethod result = Compile(x =>
            {
                x.ResolveParameterFromSource("machinename", "ENV", "MachineName");
                x.ResolveParameterFromSource("pid", "ENV", "CurrentProcessId");
            });
            TestUtility.AssertEqualWithDiffTool(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""machinename""] = (System.Object).Call Dibix.Http.Server.EnvironmentParameterSourceProvider.GetMachineName()
        ;
        $arguments.Item[""pid""] = (System.Object).Call Dibix.Http.Server.EnvironmentParameterSourceProvider.GetCurrentProcessId()
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
