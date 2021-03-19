using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Dibix.Http.Tests
{
    public partial class HttpParameterResolverTest
    {
        private static IHttpParameterResolutionMethod Compile() => Compile(x => { });
        private static IHttpParameterResolutionMethod Compile(Action<HttpActionDefinition> actionConfiguration)
        {
            HttpApiRegistration registration = new HttpApiRegistration(actionConfiguration);
            registration.Configure(null);
            HttpActionDefinition action = registration.Controllers.Single().Actions.Single();
            MethodInfo method = action.Target.Build();
            IHttpParameterResolutionMethod result = HttpParameterResolver.Compile(action, method.GetParameters());
            return result;
        }

        private sealed class HttpApiRegistration : HttpApiDescriptor
        {
            private readonly string _methodName = $"{DetermineTestName()}_Target";
            private readonly Action<HttpActionDefinition> _actionConfiguration;

            public HttpApiRegistration(Action<HttpActionDefinition> actionConfiguration) => this._actionConfiguration = actionConfiguration;

            public override void Configure(IHttpApiDiscoveryContext context) => base.RegisterController("Test", x => x.AddAction(ReflectionHttpActionTarget.Create(typeof(HttpParameterResolverTest), this._methodName), this._actionConfiguration));

            private static string DetermineTestName()
            {
                var query = from frame in new StackTrace().GetFrames()
                            let method = frame.GetMethod()
                            where method.IsDefined(typeof(FactAttribute))
                            select method.Name;
                string methodName = query.FirstOrDefault();
                if (methodName == null)
                    throw new InvalidOperationException("Could not detect test name");

                return methodName;
            }
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
            public ICollection<ExplicitHttpBodyItem> ItemsA { get; }

            public ExplicitHttpBody()
            {
                this.ItemsA = new Collection<ExplicitHttpBodyItem>();
            }
        }

        private sealed class ExplicitHttpBodyDetail
        {
            public int AgentId { get; set; }
        }

        private sealed class ExplicitHttpBodyItem
        {
            public int Id { get; }
            public string Name { get; }

            public ExplicitHttpBodyItem(int id, string name)
            {
                this.Id = id;
                this.Name = name;
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
                this.Items = new Collection<HttpBodyItem>();
            }
        }

        private sealed class HttpBodyItem
        {
            public string Password { get; }

            public HttpBodyItem(string password)
            {
                this.Password = password;
            }
        }

        private sealed class HttpBodyItemSet : StructuredType<HttpBodyItemSet, string>
        {
            public HttpBodyItemSet() : base("x") => base.ImportSqlMetadata(() => this.Add(default));

            public void Add(string encryptedpassword) => base.AddValues(encryptedpassword);
        }

        private sealed class EncryptionHttpParameterConverter : IHttpParameterConverter
        {
            public Type ExpectedInputType => typeof(string);

            public Expression ConvertValue(Expression value) => Expression.Call(typeof(EncryptionHttpParameterConverter), nameof(Convert), new Type[0], value);

            private static string Convert(string input) => $"ENCRYPTED({input})";
        }

        private sealed class ImplicitHttpBody
        {
            public short SourceId { get; set; }
            public int LocaleId { get; set; }
            public int UserId { get; set; }
            public int CannotBeMapped { get; set; }
            public ICollection<ImplicitHttpBodyItem> ItemsA { get; }

            public ImplicitHttpBody()
            {
                this.ItemsA = new Collection<ImplicitHttpBodyItem>();
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
                this.Type = type;
                this.Name = name;
            }
        }

        private sealed class ImplicitBodyHttpParameterInput
        {
            public int sourceid { get; set; }
            public int localeid { get; set; }
            public int fromuri { get; set; }
        }

        private sealed class XmlHttpParameterInput
        {
            public XElement data { get; set; }
        }
        
        private sealed class ExplicitHttpBodyItemSet : StructuredType<ExplicitHttpBodyItemSet, int, int, int, string>
        {
            public ExplicitHttpBodyItemSet() : base("x") => base.ImportSqlMetadata(() => this.Add(default, default, default, default));

            public void Add(int id_, int idx, int age_, string name_) => base.AddValues(id_, idx, age_, name_);
        }
        
        private sealed class ImplicitHttpBodyItemSet : StructuredType<ImplicitHttpBodyItemSet, short, string>
        {
            public ImplicitHttpBodyItemSet() : base("x") => base.ImportSqlMetadata(() => this.Add(default, default));

            public void Add(short type, string name) => base.AddValues(type, name);
        }
        
        private sealed class UriItemSet : StructuredType<UriItemSet, string>
        {
            public UriItemSet() : base("x") => base.ImportSqlMetadata(() => this.Add(default));

            public void Add(string name) => base.AddValues(name);
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
