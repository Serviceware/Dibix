using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;

namespace Dibix.Http.Server.Tests
{
    [TestClass]
    public partial class HttpParameterResolverTest
    {
        [TestMethod]
        public void Compile_Default()
        {
            IHttpParameterResolutionMethod method = Compile();
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters.Any());

            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage());
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(1, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_Default_Target(IDatabaseAccessorFactory databaseAccessorFactory, out int x) => x = default;

        [TestMethod]
        public void Compile_PropertySource()
        {
            HttpParameterSourceProviderRegistry.Register<LocaleParameterHttpSourceProvider>("LOCALE");
            IHttpParameterResolutionMethod method = Compile(x =>
            {
                x.ResolveParameterFromSource("lcid", "LOCALE", "LocaleId");
                x.ResolveParameterFromSource("locale", "LOCALE", "$SELF");
            });
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters.Any());

            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage());
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(3, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(1033, arguments["lcid"]);
            LocaleHttpParameterSource locale = AssertIsType<LocaleHttpParameterSource>(arguments["locale"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(1033, locale.LocaleId);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_PropertySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, int lcid, LocaleHttpParameterSource locale) { }

        [TestMethod]
        public void Compile_PropertySource_WithInvalidCast_Throws()
        {
            HttpParameterSourceProviderRegistry.Register<ApplicationHttpParameterSourceProvider>("APPLICATION");
            IHttpParameterResolutionMethod method = Compile(x => x.ResolveParameterFromSource("applicationid", "APPLICATION", "ApplicationId"));
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters.Any());

            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage());
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            Exception exception = AssertThrows<InvalidOperationException>(() => method.PrepareParameters(request, arguments, dependencyResolver.Object));
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(@"Parameter mapping failed
at GET Dibix/Test
Parameter: applicationid
Source: APPLICATION.ApplicationId", exception.Message);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(exception.InnerException);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("Null object cannot be converted to a value type.", exception.InnerException.Message);
        }
        private static void Compile_PropertySource_WithInvalidCast_Throws_Target(IDatabaseAccessorFactory databaseAccessorFactory, byte applicationid) { }

        [TestMethod]
        public void Compile_PropertySource_WithUnknownSource_Throws()
        {
            Exception exception = AssertThrows<InvalidOperationException>(() => Compile(x => x.ResolveParameterFromSource("lcid", "UNKNOWNSOURCE", "LocaleId")));
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(@"Http parameter resolver compilation failed
at GET Dibix/Test
Parameter: lcid
Source: UNKNOWNSOURCE.LocaleId", exception.Message);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(exception.InnerException);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("No source with the name 'UNKNOWNSOURCE' is registered", exception.InnerException.Message);
        }
        private static void Compile_PropertySource_WithUnknownSource_Throws_Target(IDatabaseAccessorFactory databaseAccessorFactory, int lcid) { }

        [TestMethod]
        public void Compile_ExplicitBodySource()
        {
            IHttpParameterResolutionMethod method = Compile(x =>
            {
                x.BodyContract = typeof(ExplicitHttpBody);
                x.ResolveParameterFromSource("targetid", "BODY", "SourceId");
                x.ResolveParameterFromSource("lcid", "BODY", "LocaleId");
                x.ResolveParameterFromSource("agentid", "BODY", "Detail.AgentId");
                x.ResolveParameterFromSource("skip", "BODY", "OptionalDetail.Nested.Skip");
                x.ResolveParameterFromSource("itemsa_", "BODY", "ItemsA", action =>
                {
                    action.ResolveParameterFromSource("id_", "BODY", "Detail.AgentId");
                    action.ResolveParameterFromSource("idx", "ITEM", "$INDEX");
                    action.ResolveParameterFromConstant("age_", 5);
                    action.ResolveParameterFromSource("name_", "ITEM", "Name");
                });
            });
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(1, method.Parameters.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("$body", method.Parameters["$body"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(ExplicitHttpBody), method.Parameters["$body"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.NonUser, method.Parameters["$body"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["$body"].IsOptional);

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
            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage());
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "$body", body } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(7, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(body, arguments["$body"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            ExplicitHttpBodyParameterInput input = AssertIsType<ExplicitHttpBodyParameterInput>(arguments["input"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(7, input.targetid);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(1033, arguments["lcid"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(710, arguments["agentid"]);
            StructuredType itemsa_ = AssertIsType<ExplicitHttpBodyItemSet>(arguments["itemsa_"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(@"id_ INT(4)  idx INT(4)  age_ INT(4)  name_ NVARCHAR(MAX)
----------  ----------  -----------  -------------------
710         1           5            X                  
710         2           5            Y                  ", itemsa_.Dump());
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(5, arguments["skip"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_ExplicitBodySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpBodyParameterInput input, int lcid, int agentid, ExplicitHttpBodyItemSet itemsa_, int skip = 5) { }

        [TestMethod]
        public void Compile_ImplicitBodySource()
        {
            IHttpParameterResolutionMethod method = Compile(x => x.BodyContract = typeof(ImplicitHttpBody));
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(3, method.Parameters.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("$body", method.Parameters["$body"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(ImplicitHttpBody), method.Parameters["$body"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.NonUser, method.Parameters["$body"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["$body"].IsOptional);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("id", method.Parameters["id"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(int), method.Parameters["id"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Query, method.Parameters["id"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["id"].IsOptional);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("fromuri", method.Parameters["fromuri"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(int), method.Parameters["fromuri"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Query, method.Parameters["fromuri"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["fromuri"].IsOptional);

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
            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage());
            IDictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "$body", body }
              , { "id", 2 }
              , { "fromuri", 3 }
            };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(8, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(2, arguments["id"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(5, arguments["userid"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(3, arguments["fromuri"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(body, arguments["$body"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            ImplicitBodyHttpParameterInput input = AssertIsType<ImplicitBodyHttpParameterInput>(arguments["input"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(7, input.sourceid);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(1033, input.localeid);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
            StructuredType itemsa = AssertIsType<ImplicitHttpBodyItemSet>(arguments["itemsa"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(@"type SMALLINT(2)  name NVARCHAR(MAX)
----------------  ------------------
1                 X                 
2                 Y                 ", itemsa.Dump());
            StructuredType itemsb = AssertIsType<StringSet>(arguments["itemsb"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(@"name NVARCHAR(MAX)
------------------
TextValue         ", itemsb.Dump());
        }
        private static void Compile_ImplicitBodySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, int id, [InputClass] ImplicitBodyHttpParameterInput input, int userid, ImplicitHttpBodyItemSet itemsa, StringSet itemsb) { }

        [TestMethod]
        public void Compile_BodySource_WithConverter()
        {
            HttpParameterConverterRegistry.Register<EncryptionHttpParameterConverter>("CRYPT1");
            IHttpParameterResolutionMethod method = Compile(x =>
            {
                x.BodyContract = typeof(HttpBody);
                x.ResolveParameterFromSource("encryptedpassword", "BODY", "Password", "CRYPT1");
                x.ResolveParameterFromSource("anotherencryptedpassword", "BODY", "Detail.Password", "CRYPT1");
                x.ResolveParameterFromSource("items", "BODY", "Items", action =>
                {
                    action.ResolveParameterFromSource("encryptedpassword", "ITEM", "Password", "CRYPT1");
                });
            });
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(1, method.Parameters.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("$body", method.Parameters["$body"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(HttpBody), method.Parameters["$body"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.NonUser, method.Parameters["$body"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["$body"].IsOptional);

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
            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage());
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "$body", body } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(5, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(body, arguments["$body"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("ENCRYPTED(Cake)", arguments["encryptedpassword"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("ENCRYPTED(Cookie)", arguments["anotherencryptedpassword"]);
            StructuredType items = AssertIsType<HttpBodyItemSet>(arguments["items"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(@"encryptedpassword NVARCHAR(MAX)
-------------------------------
ENCRYPTED(Item1)               
ENCRYPTED(Item2)               ", items.Dump());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_BodySource_WithConverter_Target(IDatabaseAccessorFactory databaseAccessorFactory, string encryptedpassword, string anotherencryptedpassword, HttpBodyItemSet items) { }

        [TestMethod]
        public void Compile_BodySource_Raw()
        {
            IHttpParameterResolutionMethod method = Compile(x =>
            {
                x.BodyContract = typeof(Stream);
                x.ResolveParameterFromSource("data", "BODY", "$RAW");
            });
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters.Any());

            byte[] data = { 1, 2 };
            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage { Content = new ByteArrayContent(data) });
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(2, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            MemoryStream stream = AssertIsType<MemoryStream>(arguments["data"]);
            AssertAreEqual(data, stream.ToArray());
        }
        private static void Compile_BodySource_Raw_Target(IDatabaseAccessorFactory databaseAccessorFactory, Stream data) { }
        
        [TestMethod]
        public void Compile_BodyConverter()
        {
            IHttpParameterResolutionMethod method = Compile(x =>
            {
                x.BodyContract = typeof(JObject);
                x.ResolveParameterFromBody("data", typeof(JsonToXmlConverter).AssemblyQualifiedName);
                x.ResolveParameterFromBody("value", typeof(JsonToXmlConverter).AssemblyQualifiedName);
            });
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(1, method.Parameters.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("$body", method.Parameters["$body"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(JObject), method.Parameters["$body"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.NonUser, method.Parameters["$body"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["$body"].IsOptional);

            object body = JObject.Parse("{\"id\":5}");
            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage());
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "$body", body } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(4, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(body, arguments["$body"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("<id>5</id>", arguments["value"].ToString());
            XmlHttpParameterInput input = AssertIsType<XmlHttpParameterInput>(arguments["input"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("<id>5</id>", input.data.ToString());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_BodyConverter_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] XmlHttpParameterInput input, XElement value) { }

        [TestMethod]
        public void Compile_BodyBinder()
        {
            IHttpParameterResolutionMethod method = Compile(x =>
            {
                x.BodyContract = typeof(ExplicitHttpBody);
                x.BodyBinder = typeof(FormattedInputBinder);
            });
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(1, method.Parameters.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("$body", method.Parameters["$body"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(ExplicitHttpBody), method.Parameters["$body"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.NonUser, method.Parameters["$body"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["$body"].IsOptional);

            object body = new ExplicitHttpBody { SourceId = 7 };
            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage());
            IDictionary<string, object> arguments = new Dictionary<string, object> { { "$body", body } };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(3, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(body, arguments["$body"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            ExplicitHttpBodyParameterInput input = AssertIsType<ExplicitHttpBodyParameterInput>(arguments["input"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(7, input.targetid);
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
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(@"Http parameter resolver compilation failed
at GET Dibix/Test
Parameter: input", exception.Message);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNotNull(exception.InnerException);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("Using a binder for the body is only supported if the target parameter is a class and is marked with the Dibix.InputClassAttribute", exception.InnerException.Message);
        }
        private static void Compile_BodyBinder_WithoutInputClass_Throws_Target(IDatabaseAccessorFactory databaseAccessorFactory, ExplicitHttpBodyParameterInput input) { }

        [TestMethod]
        public void Compile_ConstantSource()
        {
            IHttpParameterResolutionMethod method = Compile(x =>
            {
                x.ResolveParameterFromConstant("boolValue", true);
                x.ResolveParameterFromConstant("intValue", 2);
                x.ResolveParameterFromNull<object>("nullValue");
            });
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters.Any());

            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage());
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(4, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(true, arguments["boolValue"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(2, arguments["intValue"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(arguments["nullValue"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_ConstantSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, bool boolValue, int intValue, Guid? nullValue) { }

        [TestMethod]
        public void Compile_QuerySource()
        {
            HttpParameterConverterRegistry.Register<EncryptionHttpParameterConverter>("CRYPT2");
            IHttpParameterResolutionMethod method = Compile(x =>
            {
                x.ChildRoute = "{cake}/{fart}";
                x.ResolveParameterFromSource("true", "QUERY", "true_");
                x.ResolveParameterFromSource("name", "QUERY", "name_", "CRYPT2");
                x.ResolveParameterFromSource("targetname", "QUERY", "targetname_", "CRYPT2");
            });
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(8, method.Parameters.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("targetid", method.Parameters["targetid"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(int), method.Parameters["targetid"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Query, method.Parameters["targetid"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["targetid"].IsOptional);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("items", method.Parameters["items"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(StringSet), method.Parameters["items"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Query, method.Parameters["items"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["items"].IsOptional);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("targetname_", method.Parameters["targetname_"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(string), method.Parameters["targetname_"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Query, method.Parameters["targetname_"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["targetname_"].IsOptional);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("id", method.Parameters["id"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(int), method.Parameters["id"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Query, method.Parameters["id"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(method.Parameters["id"].IsOptional);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("anotherid", method.Parameters["anotherid"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(int), method.Parameters["anotherid"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Query, method.Parameters["anotherid"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["anotherid"].IsOptional);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("name_", method.Parameters["name_"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(string), method.Parameters["name_"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Query, method.Parameters["name_"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(method.Parameters["name_"].IsOptional);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("true_", method.Parameters["true_"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(bool?), method.Parameters["true_"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Query, method.Parameters["true_"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(method.Parameters["true_"].IsOptional);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("empty", method.Parameters["empty"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(bool?), method.Parameters["empty"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Query, method.Parameters["empty"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(method.Parameters["empty"].IsOptional);

            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage());
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

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(11, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(9, arguments["targetid"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("Muffin", arguments["targetname_"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(10, arguments["id"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(5, arguments["anotherid"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(arguments["name_"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("ENCRYPTED(Cake)", arguments["name"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(arguments["true_"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(true, arguments["true"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsNull(arguments["empty"]);
            ExplicitHttpUriParameterInput input = AssertIsType<ExplicitHttpUriParameterInput>(arguments["input"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(9, input.targetid);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("ENCRYPTED(Muffin)", input.targetname);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_QuerySource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpUriParameterInput input, StringSet items, int anotherid, int id = 0, string name = "Cake", bool? @true = true, bool? empty = null) { }

        [TestMethod]
        public void Compile_PathSource()
        {
            HttpParameterConverterRegistry.Register<EncryptionHttpParameterConverter>("CRYPT3");
            IHttpParameterResolutionMethod method = Compile(x =>
            {
                x.ChildRoute = "{targetid}/{targetname_}/{anotherid}";
                x.ResolveParameterFromSource("targetname", "PATH", "targetname_", "CRYPT3");
            });
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(3, method.Parameters.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("targetid", method.Parameters["targetid"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(int), method.Parameters["targetid"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Path, method.Parameters["targetid"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["targetid"].IsOptional);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("targetname_", method.Parameters["targetname_"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(string), method.Parameters["targetname_"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Path, method.Parameters["targetname_"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["targetname_"].IsOptional);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("anotherid", method.Parameters["anotherid"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(int), method.Parameters["anotherid"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Path, method.Parameters["anotherid"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters["anotherid"].IsOptional);

            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage());
            IDictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "targetid", 9 }
              , { "targetname_", "Muffin" }
              , { "anotherid", 5 }
            };
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(5, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(9, arguments["targetid"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("Muffin", arguments["targetname_"]);
            ExplicitHttpUriParameterInput input = AssertIsType<ExplicitHttpUriParameterInput>(arguments["input"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(9, input.targetid);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("ENCRYPTED(Muffin)", input.targetname);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_PathSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, [InputClass] ExplicitHttpUriParameterInput input, int anotherid) { }

        [TestMethod]
        public void Compile_HeaderSource()
        {
            IHttpParameterResolutionMethod method = Compile(x =>
            {
                x.ResolveParameterFromSource("authorization", "HEADER", "Authorization");
                x.ResolveParameterFromSource("tenantid", "HEADER", "X-Tenant-Id");
            });
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(2, method.Parameters.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("authorization", method.Parameters["authorization"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(string), method.Parameters["authorization"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Header, method.Parameters["authorization"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(method.Parameters["authorization"].IsOptional);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("tenantid", method.Parameters["tenantid"].Name);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(typeof(int), method.Parameters["tenantid"].Type);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(HttpParameterLocation.Header, method.Parameters["tenantid"].Location);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(method.Parameters["tenantid"].IsOptional);

            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage
            {
                Headers =
                {
                    { "Authorization", "Bearer token" },
                    { "X-Tenant-Id", "2" }
                }
            });
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(3, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            AuthenticationHeaderValue authorization = AuthenticationHeaderValue.Parse((string)arguments["authorization"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("Bearer", authorization.Scheme);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("token", authorization.Parameter);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(2, arguments["tenantid"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_HeaderSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, string authorization, int tenantid) { }

        [TestMethod]
        public void Compile_RequestSource()
        {
            IHttpParameterResolutionMethod method = Compile(x =>
            {
                x.ResolveParameterFromSource("primaryclientlanguage", "REQUEST", "Language");
                x.ResolveParameterFromSource("clientlanguages", "REQUEST", "Languages");
            });
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters.Any());

            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage { Headers = { { "Accept-Language", "en-US,en;q=0.5" } } });
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(3, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("en-US", arguments["primaryclientlanguage"]);
            StructuredType clientLanguages = AssertIsType<StringSet>(arguments["clientlanguages"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(@"name NVARCHAR(MAX)
------------------
en-US             
en                ", clientLanguages.Dump());
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_RequestSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, string primaryclientlanguage, StringSet clientlanguages) { }

        [TestMethod]
        public void Compile_EnvironmentSource()
        {
            IHttpParameterResolutionMethod method = Compile(x =>
            {
                x.ResolveParameterFromSource("machinename", "ENV", "MachineName");
                x.ResolveParameterFromSource("pid", "ENV", "CurrentProcessId");
            });
            Assert(method.Source);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(method.Parameters.Any());

            IHttpRequestDescriptor request = new HttpRequestMessageDescriptor(new HttpRequestMessage());
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            Mock<IParameterDependencyResolver> dependencyResolver = new Mock<IParameterDependencyResolver>(MockBehavior.Strict);
            Mock<IDatabaseAccessorFactory> databaseAccessorFactory = new Mock<IDatabaseAccessorFactory>(MockBehavior.Strict);

            dependencyResolver.Setup(x => x.Resolve<IDatabaseAccessorFactory>()).Returns(databaseAccessorFactory.Object);

            method.PrepareParameters(request, arguments, dependencyResolver.Object);

            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(3, arguments.Count);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(databaseAccessorFactory.Object, arguments["databaseAccessorFactory"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(Dns.GetHostEntry(String.Empty).HostName, arguments["machinename"]);
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(Process.GetCurrentProcess().Id, arguments["pid"]);
            dependencyResolver.Verify(x => x.Resolve<IDatabaseAccessorFactory>(), Times.Once);
        }
        private static void Compile_EnvironmentSource_Target(IDatabaseAccessorFactory databaseAccessorFactory, string machinename, int pid) { }
    }
}
