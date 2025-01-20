using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibix.Http.Server.Tests
{
    public partial class HttpParameterResolverTest : HttpTestBase
    {
        private HttpActionDefinition Compile() => Compile(_ => { });
        private HttpActionDefinition Compile(Action<IHttpActionDefinitionBuilder> actionConfiguration)
        {
            HttpApiRegistration registration = new HttpApiRegistration(base.TestContext.TestName, actionConfiguration) { Metadata = { AreaName = "Dibix" } };
            registration.Configure(null);
            HttpActionDefinition action = registration.Controllers.Single().Actions.Single();
            return action;
        }

        private sealed class HttpApiRegistration : HttpApiDescriptor
        {
            private readonly string _methodName;
            private readonly Action<IHttpActionDefinitionBuilder> _actionConfiguration;

            public HttpApiRegistration(string testName, Action<IHttpActionDefinitionBuilder> actionConfiguration)
            {
                _methodName = $"{testName}_Target";
                _actionConfiguration = actionConfiguration;
            }

            public override void Configure(IHttpApiDiscoveryContext context) => base.RegisterController("Test", x => x.AddAction(LocalReflectionHttpActionTarget.Create(typeof(HttpParameterResolverTest), _methodName), _actionConfiguration));
        }

        private sealed class LocaleParameterHttpSourceProvider : HttpParameterPropertySourceProvider, IHttpParameterSourceProvider
        {
            protected override Type GetInstanceType(IHttpParameterResolutionContext context) => typeof(LocaleHttpParameterSource);

            protected override Expression GetInstanceValue(Type instanceType, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter) => Expression.New(typeof(LocaleHttpParameterSource));
        }

        private sealed class LocaleHttpParameterSource
        {
            public int LocaleId => 1033;
        }

        private sealed class ApplicationHttpParameterSourceProvider : HttpParameterPropertySourceProvider, IHttpParameterSourceProvider
        {
            protected override Type GetInstanceType(IHttpParameterResolutionContext context) => typeof(ApplicationHttpParameterSource);

            protected override Expression GetInstanceValue(Type instanceType, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter) => Expression.New(typeof(ApplicationHttpParameterSource));
        }

        private sealed class ApplicationHttpParameterSource
        {
            public byte? ApplicationId { get; set; }
        }

        private sealed class ExplicitHttpBody
        {
            public short SourceId { get; set; }
            public int LocaleId { get; set; }
            public ExplicitHttpBodyDetail Detail { get; set; }
            public ExplicitHttpBodyOptionalDetail OptionalDetail { get; set; }
            public ICollection<ExplicitHttpBodyItem> ItemsA { get; }

            public ExplicitHttpBody()
            {
                ItemsA = new Collection<ExplicitHttpBodyItem>();
            }
        }

        private sealed class ExplicitHttpBodyDetail
        {
            public int AgentId { get; set; }
        }

        private sealed class ExplicitHttpBodyOptionalDetail
        {
            public ExplicitHttpBodyOptionalDetailNested Nested { get; set; }
        }

        private sealed class ExplicitHttpBodyOptionalDetailNested
        {
            public int Skip { get; set; }
            public int? Take { get; set; }
        }

        private sealed class ExplicitHttpBodyItem
        {
            public int Id { get; }
            public string Name { get; }

            public ExplicitHttpBodyItem(int id, string name)
            {
                Id = id;
                Name = name;
            }
        }

        private sealed class ExplicitHttpBodyParameterInput
        {
            public int targetid { get; set; }
        }

        private sealed class ExplicitHttpUriParameterInput
        {
            public int targetid { get; set; }
            public string targetname { get; set; }
        }

        private sealed class HttpBodyDetail
        {
            public string Password { get; set; }
        }

        private sealed class HttpBody
        {
            public string Password { get; set; }
            public HttpBodyDetail Detail { get; set; }
            public ICollection<HttpBodyItem> Items { get; }

            public HttpBody()
            {
                Items = new Collection<HttpBodyItem>();
            }
        }

        private sealed class HttpBodyItem
        {
            public string Password { get; }

            public HttpBodyItem(string password)
            {
                Password = password;
            }
        }

        private sealed class HttpBodyItemSet : StructuredType<HttpBodyItemSet>
        {
            public override string TypeName => "x";

            public void Add(string encryptedpassword) => AddRecord(encryptedpassword);

            protected override void CollectMetadata(ISqlMetadataCollector collector)
            {
                collector.RegisterMetadata("encryptedpassword", SqlDbType.NVarChar, maxLength: -1);
            }
        }

        private sealed class EncryptionHttpParameterConverter : IHttpParameterConverter
        {
            public Type ExpectedInputType => typeof(string);

            public Expression ConvertValue(Expression value, IHttpParameterConversionContext context) => Expression.Call(typeof(EncryptionHttpParameterConverter), nameof(Convert), Type.EmptyTypes, value);

            private static string Convert(string input) => $"ENCRYPTED({input})";
        }

        private sealed class ImplicitHttpBody
        {
            public short SourceId { get; set; }
            public int LocaleId { get; set; }
            public int UserId { get; set; }
            public int CannotBeMapped { get; set; }
            public ICollection<ImplicitHttpBodyItem> ItemsA { get; }
            public ICollection<string> ItemsB { get; }

            public ImplicitHttpBody()
            {
                ItemsA = new Collection<ImplicitHttpBodyItem>();
                ItemsB = new Collection<string>();
            }
        }

        private enum ImplicitHttpBodyItemType
        {
            A = 1,
            B = 2,
        }

        private sealed class ImplicitHttpBodyItem
        {
            public ImplicitHttpBodyItemType Type { get; }
            public string Name { get; }

            public ImplicitHttpBodyItem(ImplicitHttpBodyItemType type, string name)
            {
                Type = type;
                Name = name;
            }
        }

        private sealed class ImplicitBodyHttpParameterInput
        {
            public int? sourceid { get; set; }
            public int localeid { get; set; }
            public int fromuri { get; set; }
        }

        private sealed class XmlHttpParameterInput
        {
            public XElement data { get; set; }
        }
        
        private sealed class ExplicitHttpBodyItemSet : StructuredType<ExplicitHttpBodyItemSet>
        {
            public override string TypeName => "x";

            public void Add(int id_, int idx, int age_, string name_) => AddRecord(id_, idx, age_, name_);

            protected override void CollectMetadata(ISqlMetadataCollector collector)
            {
                collector.RegisterMetadata("id_", SqlDbType.Int);
                collector.RegisterMetadata("idx", SqlDbType.Int);
                collector.RegisterMetadata("age_", SqlDbType.Int);
                collector.RegisterMetadata("name_", SqlDbType.NVarChar, maxLength: -1);
            }
        }
        
        private sealed class ImplicitHttpBodyItemSet : StructuredType<ImplicitHttpBodyItemSet>
        {
            public override string TypeName => "x";

            public void Add(short type, string name) => AddRecord(type, name);

            protected override void CollectMetadata(ISqlMetadataCollector collector)
            {
                collector.RegisterMetadata("type", SqlDbType.SmallInt);
                collector.RegisterMetadata("name", SqlDbType.NVarChar, maxLength: -1);
            }
        }
        
        private sealed class StringSet : StructuredType<StringSet>
        {
            public override string TypeName => "x";

            public void Add(string name) => AddRecord(name);

            protected override void CollectMetadata(ISqlMetadataCollector collector)
            {
                collector.RegisterMetadata("name", SqlDbType.NVarChar, maxLength: -1);
            }
        }

        private sealed class JsonToXmlConverter : IFormattedInputConverter<JObject, XElement>
        {
            public XElement Convert(JObject source) => JsonConvert.DeserializeXNode(source.ToString()).Root;
        }

        private sealed class FormattedInputBinder : IFormattedInputBinder<ExplicitHttpBody, ExplicitHttpBodyParameterInput>
        {
            public void Bind(ExplicitHttpBody source, ExplicitHttpBodyParameterInput target) => target.targetid = source.SourceId;
        }
    }
}
