using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;
using Dibix.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;

namespace Dibix.Http.Server.Tests
{
    [TestClass]
    public partial class HttpParameterResolverTest : TestBase
    {
        [TestMethod]
        public void Compile_Default()
        {
            IHttpParameterResolutionMethod result = this.Compile();
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource
    }
}", result.Source);
            Assert.IsFalse(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.AreEqual(1, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }

        private static void Compile_Default_Target(IDatabaseAccessorFactory databaseAccessorFactory, out int x) => x = default;

        [TestMethod]
        public void Compile_PropertySource()
        {
            HttpParameterSourceProviderRegistry.Register<LocaleParameterHttpSourceProvider>("LOCALE");
            IHttpParameterResolutionMethod result = this.Compile(x =>
            {
                x.ResolveParameterFromSource("lcid", "LOCALE", "LocaleId");
                x.ResolveParameterFromSource("locale", "LOCALE", "$SELF");
            });
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
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
            Assert.IsFalse(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.AreEqual(3, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.AreEqual(1033, arguments["lcid"]);
            LocaleHttpParameterSource locale = AssertIsType<LocaleHttpParameterSource>(arguments["locale"]);
            Assert.AreEqual(1033, locale.LocaleId);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_PropertySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, int lcid, LocaleHttpParameterSource locale) { }

        [TestMethod]
        public void Compile_PropertySource_WithInvalidCast_Throws()
        {
            HttpParameterSourceProviderRegistry.Register<ApplicationHttpParameterSourceProvider>("APPLICATION");
            IHttpParameterResolutionMethod result = this.Compile(x => x.ResolveParameterFromSource("applicationid", "APPLICATION", "ApplicationId"));
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+ApplicationHttpParameterSource $applicationSource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $applicationSource = .New Dibix.Http.Server.Tests.HttpParameterResolverTest+ApplicationHttpParameterSource();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""applicationid""] = (System.Object).Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
            ""applicationid"",
            $applicationSource.ApplicationId,
            $action)
    }
}", result.Source);
            Assert.IsFalse(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            Exception exception = AssertThrows<InvalidOperationException>(() => result.PrepareParameters(request, arguments, dependencyResolver.Object));
            Assert.AreEqual(@"Parameter mapping failed
at GET Dibix/Test
Parameter: applicationid
Source: APPLICATION.ApplicationId", exception.Message);
            Assert.IsNotNull(exception.InnerException);
            Assert.AreEqual("Null object cannot be converted to a value type.", exception.InnerException.Message);
        }
        private static void Compile_PropertySource_WithInvalidCast_Throws_Target(IDatabaseAccessorFactory databaseAccessorFactory, byte applicationid) { }

        [TestMethod]
        public void Compile_PropertySource_WithUnknownSource_Throws()
        {
            Exception exception = AssertThrows<InvalidOperationException>(() => Compile(x => x.ResolveParameterFromSource("lcid", "UNKNOWNSOURCE", "LocaleId")));
            Assert.AreEqual(@"Http parameter resolver compilation failed
at GET Dibix/Test
Parameter: lcid
Source: UNKNOWNSOURCE.LocaleId", exception.Message);
            Assert.IsNotNull(exception.InnerException);
            Assert.AreEqual("No source with the name 'UNKNOWNSOURCE' is registered", exception.InnerException.Message);
        }
        private static void Compile_PropertySource_WithUnknownSource_Throws_Target(IDatabaseAccessorFactory databaseAccessorFactory, int lcid) { }

        [TestMethod]
        public void Compile_ExplicitBodySource()
        {
            IHttpParameterResolutionMethod result = this.Compile(x =>
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
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
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
            $bodySource.SourceId,
            $action);
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
            Assert.AreEqual(1, result.Parameters.Count);
            Assert.AreEqual("$body", result.Parameters["$body"].Name);
            Assert.AreEqual(typeof(ExplicitHttpBody), result.Parameters["$body"].Type);
            Assert.AreEqual(HttpParameterLocation.NonUser, result.Parameters["$body"].Location);
            Assert.IsFalse(result.Parameters["$body"].IsOptional);

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

            Assert.AreEqual(7, arguments.Count);
            Assert.AreEqual(body, arguments["$body"]);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            ExplicitHttpBodyParameterInput input = AssertIsType<ExplicitHttpBodyParameterInput>(arguments["input"]);
            Assert.AreEqual(7, input.targetid);
            Assert.AreEqual(1033, arguments["lcid"]);
            Assert.AreEqual(710, arguments["agentid"]);
            StructuredType itemsa_ = AssertIsType<ExplicitHttpBodyItemSet>(arguments["itemsa_"]);
            Assert.AreEqual(@"id_ INT(4)  idx INT(4)  age_ INT(4)  name_ NVARCHAR(MAX)
----------  ----------  -----------  -------------------
710         1           5            X                  
710         2           5            Y                  ", itemsa_.Dump());
            Assert.AreEqual(5, arguments["skip"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_ExplicitBodySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpBodyParameterInput input, int lcid, int agentid, ExplicitHttpBodyItemSet itemsa_, int skip = 5) { }

        [TestMethod]
        public void Compile_ImplicitBodySource()
        {
            IHttpParameterResolutionMethod result = this.Compile(x => x.BodyContract = typeof(ImplicitHttpBody));
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
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
            .Lambda #Lambda2<System.Action`2[Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet,Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItem]>)
        ;
        $arguments.Item[""itemsb""] = (System.Object).Call Dibix.StructuredType`1[Dibix.Http.Server.Tests.HttpParameterResolverTest+StringSet].From(
            $bodySource.ItemsB,
            .Lambda #Lambda3<System.Action`2[Dibix.Http.Server.Tests.HttpParameterResolverTest+StringSet,System.String]>);
        $input = .New Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitBodyHttpParameterInput();
        $input.sourceid = .Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
            ""sourceid"",
            $bodySource.SourceId,
            $action);
        $input.localeid = $bodySource.LocaleId;
        $input.fromuri = .Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
            ""fromuri"",
            $arguments.Item[""fromuri""],
            $action);
        $arguments.Item[""input""] = (System.Object)$input
    }
}

.Lambda #Lambda2<System.Action`2[Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet,Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItem]>(
    Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItemSet $x,
    Dibix.Http.Server.Tests.HttpParameterResolverTest+ImplicitHttpBodyItem $y) {
    .Call $x.Add(
        .Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
            ""type"",
            $y.Type,
            $action),
        $y.Name)
}

.Lambda #Lambda3<System.Action`2[Dibix.Http.Server.Tests.HttpParameterResolverTest+StringSet,System.String]>(
    Dibix.Http.Server.Tests.HttpParameterResolverTest+StringSet $x,
    System.String $y) {
    .Call $x.Add($y)
}", result.Source);
            Assert.AreEqual(3, result.Parameters.Count);
            Assert.AreEqual("$body", result.Parameters["$body"].Name);
            Assert.AreEqual(typeof(ImplicitHttpBody), result.Parameters["$body"].Type);
            Assert.AreEqual(HttpParameterLocation.NonUser, result.Parameters["$body"].Location);
            Assert.IsFalse(result.Parameters["$body"].IsOptional);
            Assert.AreEqual("id", result.Parameters["id"].Name);
            Assert.AreEqual(typeof(int), result.Parameters["id"].Type);
            Assert.AreEqual(HttpParameterLocation.Query, result.Parameters["id"].Location);
            Assert.IsFalse(result.Parameters["id"].IsOptional);
            Assert.AreEqual("fromuri", result.Parameters["fromuri"].Name);
            Assert.AreEqual(typeof(int), result.Parameters["fromuri"].Type);
            Assert.AreEqual(HttpParameterLocation.Query, result.Parameters["fromuri"].Location);
            Assert.IsFalse(result.Parameters["fromuri"].IsOptional);

            object body = new ImplicitHttpBody
            {
                SourceId = 7,
                LocaleId = 1033,
                UserId = 5,
                ItemsA =
                {
                    new ImplicitHttpBodyItem(ImplicitHttpBodyItemType.A, "X"),
                    new ImplicitHttpBodyItem(ImplicitHttpBodyItemType.B, "Y")
                },
                ItemsB = { "TextValue" }
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

            Assert.AreEqual(8, arguments.Count);
            Assert.AreEqual(2, arguments["id"]);
            Assert.AreEqual(5, arguments["userid"]);
            Assert.AreEqual(3, arguments["fromuri"]);
            Assert.AreEqual(body, arguments["$body"]);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            ImplicitBodyHttpParameterInput input = AssertIsType<ImplicitBodyHttpParameterInput>(arguments["input"]);
            Assert.AreEqual(7, input.sourceid);
            Assert.AreEqual(1033, input.localeid);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
            StructuredType itemsa = AssertIsType<ImplicitHttpBodyItemSet>(arguments["itemsa"]);
            Assert.AreEqual(@"type SMALLINT(2)  name NVARCHAR(MAX)
----------------  ------------------
1                 X                 
2                 Y                 ", itemsa.Dump());
            StructuredType itemsb = AssertIsType<StringSet>(arguments["itemsb"]);
            Assert.AreEqual(@"name NVARCHAR(MAX)
------------------
TextValue         ", itemsb.Dump());
        }
        private static void Compile_ImplicitBodySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, int id, [InputClass] ImplicitBodyHttpParameterInput input, int userid, ImplicitHttpBodyItemSet itemsa, StringSet itemsb) { }

        [TestMethod]
        public void Compile_BodySource_WithConverter()
        {
            HttpParameterConverterRegistry.Register<EncryptionHttpParameterConverter>("CRYPT1");
            IHttpParameterResolutionMethod result = this.Compile(x =>
            {
                x.BodyContract = typeof(HttpBody);
                x.ResolveParameterFromSource("encryptedpassword", "BODY", "Password", "CRYPT1");
                x.ResolveParameterFromSource("anotherencryptedpassword", "BODY", "Detail.Password", "CRYPT1");
                x.ResolveParameterFromSource("items", "BODY", "Items", y =>
                {
                    y.ResolveParameterFromSource("encryptedpassword", "ITEM", "Password", "CRYPT1");
                });
            });
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
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
            .Lambda #Lambda2<System.Action`2[Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItemSet,Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItem]>)
    }
}

.Lambda #Lambda2<System.Action`2[Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItemSet,Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItem]>(
    Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItemSet $x,
    Dibix.Http.Server.Tests.HttpParameterResolverTest+HttpBodyItem $y) {
    .Call $x.Add(.Call Dibix.Http.Server.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert($y.Password)
    )
}", result.Source);
            Assert.AreEqual(1, result.Parameters.Count);
            Assert.AreEqual("$body", result.Parameters["$body"].Name);
            Assert.AreEqual(typeof(HttpBody), result.Parameters["$body"].Type);
            Assert.AreEqual(HttpParameterLocation.NonUser, result.Parameters["$body"].Location);
            Assert.IsFalse(result.Parameters["$body"].IsOptional);

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

            Assert.AreEqual(5, arguments.Count);
            Assert.AreEqual(body, arguments["$body"]);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.AreEqual("ENCRYPTED(Cake)", arguments["encryptedpassword"]);
            Assert.AreEqual("ENCRYPTED(Cookie)", arguments["anotherencryptedpassword"]);
            StructuredType items = AssertIsType<HttpBodyItemSet>(arguments["items"]);
            Assert.AreEqual(@"encryptedpassword NVARCHAR(MAX)
-------------------------------
ENCRYPTED(Item1)               
ENCRYPTED(Item2)               ", items.Dump());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_BodySource_WithConverter_Target(IDatabaseAccessorFactory databaseAccessorFactory, string encryptedpassword, string anotherencryptedpassword, HttpBodyItemSet items) { }

        [TestMethod]
        public void Compile_BodySource_Raw()
        {
            IHttpParameterResolutionMethod result = this.Compile(x =>
            {
                x.BodyContract = typeof(Stream);
                x.ResolveParameterFromSource("data", "BODY", "$RAW");
            });
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""data""] = (System.Object).Call (.Call (.Call ($request.Content).ReadAsStreamAsync()).GetAwaiter()).GetResult()
    }
}", result.Source);
            Assert.IsFalse(result.Parameters.Any());

            byte[] data = { 1, 2 };
            HttpRequestMessage request = new HttpRequestMessage();
            request.Content = new ByteArrayContent(data);
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.AreEqual(2, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            MemoryStream stream = AssertIsType<MemoryStream>(arguments["data"]);
            AssertAreEqual(data, stream.ToArray());
        }
        private static void Compile_BodySource_Raw_Target(IDatabaseAccessorFactory databaseAccessorFactory, Stream data) { }
        
        [TestMethod]
        public void Compile_BodyConverter()
        {
            IHttpParameterResolutionMethod result = this.Compile(x =>
            {
                x.BodyContract = typeof(JObject);
                x.ResolveParameterFromBody("data", typeof(JsonToXmlConverter).AssemblyQualifiedName);
                x.ResolveParameterFromBody("value", typeof(JsonToXmlConverter).AssemblyQualifiedName);
            });
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
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
            Assert.AreEqual(1, result.Parameters.Count);
            Assert.AreEqual("$body", result.Parameters["$body"].Name);
            Assert.AreEqual(typeof(JObject), result.Parameters["$body"].Type);
            Assert.AreEqual(HttpParameterLocation.NonUser, result.Parameters["$body"].Location);
            Assert.IsFalse(result.Parameters["$body"].IsOptional);

            object body = JObject.Parse("{\"id\":5}");
            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "$body", body } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.AreEqual(4, arguments.Count);
            Assert.AreEqual(body, arguments["$body"]);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.AreEqual("<id>5</id>", arguments["value"].ToString());
            XmlHttpParameterInput input = AssertIsType<XmlHttpParameterInput>(arguments["input"]);
            Assert.AreEqual("<id>5</id>", input.data.ToString());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_BodyConverter_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] XmlHttpParameterInput input, XElement value) { }

        [TestMethod]
        public void Compile_BodyBinder()
        {
            IHttpParameterResolutionMethod result = this.Compile(x =>
            {
                x.BodyContract = typeof(ExplicitHttpBody);
                x.BodyBinder = typeof(FormattedInputBinder);
            });
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
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
            Assert.AreEqual(1, result.Parameters.Count);
            Assert.AreEqual("$body", result.Parameters["$body"].Name);
            Assert.AreEqual(typeof(ExplicitHttpBody), result.Parameters["$body"].Type);
            Assert.AreEqual(HttpParameterLocation.NonUser, result.Parameters["$body"].Location);
            Assert.IsFalse(result.Parameters["$body"].IsOptional);

            object body = new ExplicitHttpBody { SourceId = 7 };
            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "$body", body } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.AreEqual(3, arguments.Count);
            Assert.AreEqual(body, arguments["$body"]);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            ExplicitHttpBodyParameterInput input = AssertIsType<ExplicitHttpBodyParameterInput>(arguments["input"]);
            Assert.AreEqual(7, input.targetid);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_BodyBinder_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpBodyParameterInput input) { }

        [TestMethod]
        public void Compile_BodyBinder_WithoutInputClass_Throws()
        {
            Exception exception = AssertThrows<InvalidOperationException>(() => Compile(x =>
            {
                x.BodyContract = typeof(ExplicitHttpBody);
                x.BodyBinder = typeof(FormattedInputBinder);
            }));
            Assert.AreEqual(@"Http parameter resolver compilation failed
at GET Dibix/Test
Parameter: input", exception.Message);
            Assert.IsNotNull(exception.InnerException);
            Assert.AreEqual("Using a binder for the body is only supported if the target parameter is a class and is marked with the Dibix.InputClassAttribute", exception.InnerException.Message);
        }
        private static void Compile_BodyBinder_WithoutInputClass_Throws_Target(IDatabaseAccessorFactory databaseAccessorFactory, ExplicitHttpBodyParameterInput input) { }

        [TestMethod]
        public void Compile_ConstantSource()
        {
            IHttpParameterResolutionMethod result = this.Compile(x =>
            {
                x.ResolveParameterFromConstant("boolValue", true);
                x.ResolveParameterFromConstant("intValue", 2);
                x.ResolveParameterFromNull("nullValue");
            });
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""boolValue""] = (System.Object)True;
        $arguments.Item[""intValue""] = (System.Object)2;
        $arguments.Item[""nullValue""] = (System.Object)null
    }
}", result.Source);
            Assert.IsFalse(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.AreEqual(4, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.AreEqual(true, arguments["boolValue"]);
            Assert.AreEqual(2, arguments["intValue"]);
            Assert.IsNull(arguments["nullValue"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_ConstantSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, bool boolValue, int intValue, Guid? nullValue) { }

        [TestMethod]
        public void Compile_QuerySource()
        {
            HttpParameterConverterRegistry.Register<EncryptionHttpParameterConverter>("CRYPT2");
            IHttpParameterResolutionMethod result = this.Compile(x =>
            {
                x.ChildRoute = "{cake}/{fart}";
                x.ResolveParameterFromSource("true", "QUERY", "true_");
                x.ResolveParameterFromSource("name", "QUERY", "name_", "CRYPT2");
                x.ResolveParameterFromSource("targetname", "QUERY", "targetname_", "CRYPT2");
            });
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
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
                $arguments.Item[""name""],
                $action));
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
            $arguments.Item[""targetid""],
            $action);
        $input.targetname = .Call Dibix.Http.Server.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert(.Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
                ""targetname"",
                $arguments.Item[""targetname_""],
                $action));
        $arguments.Item[""input""] = (System.Object)$input
    }
}", result.Source);
            Assert.AreEqual(8, result.Parameters.Count);
            Assert.AreEqual("targetid", result.Parameters["targetid"].Name);
            Assert.AreEqual(typeof(int), result.Parameters["targetid"].Type);
            Assert.AreEqual(HttpParameterLocation.Query, result.Parameters["targetid"].Location);
            Assert.IsFalse(result.Parameters["targetid"].IsOptional);
            Assert.AreEqual("items", result.Parameters["items"].Name);
            Assert.AreEqual(typeof(StringSet), result.Parameters["items"].Type);
            Assert.AreEqual(HttpParameterLocation.Query, result.Parameters["items"].Location);
            Assert.IsFalse(result.Parameters["items"].IsOptional);
            Assert.AreEqual("targetname_", result.Parameters["targetname_"].Name);
            Assert.AreEqual(typeof(string), result.Parameters["targetname_"].Type);
            Assert.AreEqual(HttpParameterLocation.Query, result.Parameters["targetname_"].Location);
            Assert.IsFalse(result.Parameters["targetname_"].IsOptional);
            Assert.AreEqual("id", result.Parameters["id"].Name);
            Assert.AreEqual(typeof(int), result.Parameters["id"].Type);
            Assert.AreEqual(HttpParameterLocation.Query, result.Parameters["id"].Location);
            Assert.IsTrue(result.Parameters["id"].IsOptional);
            Assert.AreEqual("anotherid", result.Parameters["anotherid"].Name);
            Assert.AreEqual(typeof(int), result.Parameters["anotherid"].Type);
            Assert.AreEqual(HttpParameterLocation.Query, result.Parameters["anotherid"].Location);
            Assert.IsFalse(result.Parameters["anotherid"].IsOptional);
            Assert.AreEqual("name_", result.Parameters["name_"].Name);
            Assert.AreEqual(typeof(string), result.Parameters["name_"].Type);
            Assert.AreEqual(HttpParameterLocation.Query, result.Parameters["name_"].Location);
            Assert.IsTrue(result.Parameters["name_"].IsOptional);
            Assert.AreEqual("true_", result.Parameters["true_"].Name);
            Assert.AreEqual(typeof(bool?), result.Parameters["true_"].Type);
            Assert.AreEqual(HttpParameterLocation.Query, result.Parameters["true_"].Location);
            Assert.IsTrue(result.Parameters["true_"].IsOptional);
            Assert.AreEqual("empty", result.Parameters["empty"].Name);
            Assert.AreEqual(typeof(bool?), result.Parameters["empty"].Type);
            Assert.AreEqual(HttpParameterLocation.Query, result.Parameters["empty"].Location);
            Assert.IsTrue(result.Parameters["empty"].IsOptional);

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

            Assert.AreEqual(11, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.AreEqual(9, arguments["targetid"]);
            Assert.AreEqual("Muffin", arguments["targetname_"]);
            Assert.AreEqual(10, arguments["id"]);
            Assert.AreEqual(5, arguments["anotherid"]);
            Assert.IsNull(arguments["name_"]);
            Assert.AreEqual("ENCRYPTED(Cake)", arguments["name"]);
            Assert.IsNull(arguments["true_"]);
            Assert.AreEqual(true, arguments["true"]);
            Assert.IsNull(arguments["empty"]);
            ExplicitHttpUriParameterInput input = AssertIsType<ExplicitHttpUriParameterInput>(arguments["input"]);
            Assert.AreEqual(9, input.targetid);
            Assert.AreEqual("ENCRYPTED(Muffin)", input.targetname);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_QuerySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpUriParameterInput input, StringSet items, int anotherid, int id = 0, string name = "Cake", bool? @true = true, bool? empty = null) { }

        [TestMethod]
        public void Compile_PathSource()
        {
            HttpParameterConverterRegistry.Register<EncryptionHttpParameterConverter>("CRYPT3");
            IHttpParameterResolutionMethod result = this.Compile(x =>
            {
                x.ChildRoute = "{targetid}/{targetname_}/{anotherid}";
                x.ResolveParameterFromSource("targetname", "PATH", "targetname_", "CRYPT3");
            });
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
    .Block(
        Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource,
        Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpUriParameterInput $input) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $input = .New Dibix.Http.Server.Tests.HttpParameterResolverTest+ExplicitHttpUriParameterInput();
        $input.targetid = .Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
            ""targetid"",
            $arguments.Item[""targetid""],
            $action);
        $input.targetname = .Call Dibix.Http.Server.Tests.HttpParameterResolverTest+EncryptionHttpParameterConverter.Convert(.Call Dibix.Http.Server.HttpParameterResolver.ConvertValue(
                ""targetname"",
                $arguments.Item[""targetname_""],
                $action));
        $arguments.Item[""input""] = (System.Object)$input
    }
}", result.Source);
            Assert.AreEqual(3, result.Parameters.Count);
            Assert.AreEqual("targetid", result.Parameters["targetid"].Name);
            Assert.AreEqual(typeof(int), result.Parameters["targetid"].Type);
            Assert.AreEqual(HttpParameterLocation.Path, result.Parameters["targetid"].Location);
            Assert.IsFalse(result.Parameters["targetid"].IsOptional);
            Assert.AreEqual("targetname_", result.Parameters["targetname_"].Name);
            Assert.AreEqual(typeof(string), result.Parameters["targetname_"].Type);
            Assert.AreEqual(HttpParameterLocation.Path, result.Parameters["targetname_"].Location);
            Assert.IsFalse(result.Parameters["targetname_"].IsOptional);
            Assert.AreEqual("anotherid", result.Parameters["anotherid"].Name);
            Assert.AreEqual(typeof(int), result.Parameters["anotherid"].Type);
            Assert.AreEqual(HttpParameterLocation.Path, result.Parameters["anotherid"].Location);
            Assert.IsFalse(result.Parameters["anotherid"].IsOptional);

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

            Assert.AreEqual(5, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.AreEqual(9, arguments["targetid"]);
            Assert.AreEqual("Muffin", arguments["targetname_"]);
            ExplicitHttpUriParameterInput input = AssertIsType<ExplicitHttpUriParameterInput>(arguments["input"]);
            Assert.AreEqual(9, input.targetid);
            Assert.AreEqual("ENCRYPTED(Muffin)", input.targetname);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_PathSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpUriParameterInput input, int anotherid) { }

        [TestMethod]
        public void Compile_HeaderSource()
        {
            IHttpParameterResolutionMethod result = this.Compile(x =>
            {
                x.ResolveParameterFromSource("authorization", "HEADER", "Authorization");
                x.ResolveParameterFromSource("tenantid", "HEADER", "X-Tenant-Id");
            });
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
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
                ""X-Tenant-Id""),
            $action)
    }
}", result.Source);
            Assert.AreEqual(2, result.Parameters.Count);
            Assert.AreEqual("authorization", result.Parameters["authorization"].Name);
            Assert.AreEqual(typeof(string), result.Parameters["authorization"].Type);
            Assert.AreEqual(HttpParameterLocation.Header, result.Parameters["authorization"].Location);
            Assert.IsTrue(result.Parameters["authorization"].IsOptional);
            Assert.AreEqual("tenantid", result.Parameters["tenantid"].Name);
            Assert.AreEqual(typeof(int), result.Parameters["tenantid"].Type);
            Assert.AreEqual(HttpParameterLocation.Header, result.Parameters["tenantid"].Location);
            Assert.IsTrue(result.Parameters["tenantid"].IsOptional);

            HttpRequestMessage request = new HttpRequestMessage();
            request.Headers.Add("Authorization", "Bearer token");
            request.Headers.Add("X-Tenant-Id", "2");
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.AreEqual(3, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            AuthenticationHeaderValue authorization = AuthenticationHeaderValue.Parse((string)arguments["authorization"]);
            Assert.AreEqual("Bearer", authorization.Scheme);
            Assert.AreEqual("token", authorization.Parameter);
            Assert.AreEqual(2, arguments["tenantid"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_HeaderSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, string authorization, int tenantid) { }

        [TestMethod]
        public void Compile_RequestSource()
        {
            IHttpParameterResolutionMethod result = this.Compile(x =>
            {
                x.ResolveParameterFromSource("primaryclientlanguage", "REQUEST", "Language");
                x.ResolveParameterFromSource("clientlanguages", "REQUEST", "Languages");
            });
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
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
            Assert.IsFalse(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.AreEqual(3, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.AreEqual("en-US", arguments["primaryclientlanguage"]);
            StructuredType clientLanguages = AssertIsType<StringSet>(arguments["clientlanguages"]);
            Assert.AreEqual(@"name NVARCHAR(MAX)
------------------
en-US             
en                ", clientLanguages.Dump());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_RequestSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, string primaryclientlanguage, StringSet clientlanguages) { }

        [TestMethod]
        public void Compile_EnvironmentSource()
        {
            IHttpParameterResolutionMethod result = this.Compile(x =>
            {
                x.ResolveParameterFromSource("machinename", "ENV", "MachineName");
                x.ResolveParameterFromSource("pid", "ENV", "CurrentProcessId");
            });
            this.AssertEqual(@".Lambda #Lambda1<Dibix.Http.Server.HttpParameterResolver+ResolveParameters>(
    System.Net.Http.HttpRequestMessage $request,
    System.Collections.Generic.IDictionary`2[System.String,System.Object] $arguments,
    Dibix.Http.Server.IParameterDependencyResolver $dependencyResolver,
    Dibix.Http.Server.HttpActionDefinition $action) {
    .Block(Dibix.IDatabaseAccessorFactory $databaseaccessorfactorySource) {
        $databaseaccessorfactorySource = .Call $dependencyResolver.Resolve();
        $arguments.Item[""databaseAccessorFactory""] = (System.Object)$databaseaccessorfactorySource;
        $arguments.Item[""machinename""] = (System.Object).Call Dibix.Http.Server.EnvironmentParameterSourceProvider.GetMachineName()
        ;
        $arguments.Item[""pid""] = (System.Object).Call Dibix.Http.Server.EnvironmentParameterSourceProvider.GetCurrentProcessId()
    }
}", result.Source);
            Assert.IsFalse(result.Parameters.Any());

            HttpRequestMessage request = new HttpRequestMessage();
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            result.PrepareParameters(request, arguments, dependencyResolver.Object);

            Assert.AreEqual(3, arguments.Count);
            Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Assert.AreEqual(Dns.GetHostEntry(String.Empty).HostName, arguments["machinename"]);
            Assert.AreEqual(Process.GetCurrentProcess().Id, arguments["pid"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_EnvironmentSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, string machinename, int pid) { }
    }
}
