using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using Dibix.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Dibix.Tests
{
    public partial class HttpParameterResolverTest
    {
        private static IHttpParameterResolutionMethod Compile() => Compile(x => { });
        private static IHttpParameterResolutionMethod Compile(Action<HttpActionDefinition> actionConfiguration)
        {
            HttpApiRegistration registration = new HttpApiRegistration(actionConfiguration);
            registration.Configure();
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

            public override void Configure() => base.RegisterController("Test", x => x.AddAction(ReflectionHttpActionTarget.Create(typeof(HttpParameterResolverTest), this._methodName), this._actionConfiguration));

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

        private sealed class LocaleParameterHttpSourceProvider : IHttpParameterSourceProvider
        {
            public Type GetInstanceType(HttpActionDefinition action) => typeof(LocaleHttpParameterSource);

            public Expression GetInstanceValue(Type instanceType, ParameterExpression requestParameter, ParameterExpression argumentsParameter, ParameterExpression dependencyProviderParameter) => Expression.New(typeof(LocaleHttpParameterSource));
        }

        private sealed class LocaleHttpParameterSource
        {
            public int LocaleId => 1033;
        }

        private sealed class ApplicationHttpParameterSourceProvider : IHttpParameterSourceProvider
        {
            public Type GetInstanceType(HttpActionDefinition action) => typeof(ApplicationHttpParameterSource);

            public Expression GetInstanceValue(Type instanceType, ParameterExpression requestParameter, ParameterExpression argumentsParameter, ParameterExpression dependencyProviderParameter) => Expression.New(typeof(ApplicationHttpParameterSource));
        }

        private sealed class ApplicationHttpParameterSource
        {
            public byte? ApplicationId { get; set; }
        }

        private sealed class ExplicitHttpBody
        {
            public short SourceId { get; set; }
            public int LocaleId { get; set; }
            public ICollection<ExplicitHttpBodyItem> ItemsA { get; }

            public ExplicitHttpBody()
            {
                this.ItemsA = new Collection<ExplicitHttpBodyItem>();
            }
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

        private sealed class ExplicitHttpParameterInput
        {
            public int targetid { get; set; }
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

        private sealed class ImplicitHttpBodyItem
        {
            public int Id { get; }
            public string Name { get; }

            public ImplicitHttpBodyItem(int id, string name)
            {
                this.Id = id;
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
        
        private sealed class ImplicitHttpBodyItemSet : StructuredType<ImplicitHttpBodyItemSet, int, string>
        {
            public ImplicitHttpBodyItemSet() : base("x") => base.ImportSqlMetadata(() => this.Add(default, default));

            public void Add(int id, string name) => base.AddValues(id, name);
        }

        private sealed class JsonToXmlConverter : IFormattedInputConverter<JObject, XElement>
        {
            public XElement Convert(JObject source) => JsonConvert.DeserializeXNode(source.ToString()).Root;
        }

        private sealed class FormattedInputBinder : IFormattedInputBinder<ExplicitHttpBody, ExplicitHttpParameterInput>
        {
            public void Bind(ExplicitHttpBody source, ExplicitHttpParameterInput target) => target.targetid = source.SourceId;
        }
    }
}
