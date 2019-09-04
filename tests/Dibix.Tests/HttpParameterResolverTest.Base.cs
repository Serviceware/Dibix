using System;
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

        private sealed class HttpParameterSourceProvider : IHttpParameterSourceProvider
        {
            public Type GetInstanceType(HttpActionDefinition action) => typeof(HttpParameterSource);

            public Expression GetInstanceValue(Type instanceType, ParameterExpression argumentsParameter, ParameterExpression dependencyProviderParameter) => Expression.New(typeof(HttpParameterSource));
        }

        private sealed class HttpParameterSource
        {
            public int LocaleId => 1033;
        }

        private sealed class HttpBody
        {
            public int SourceId { get; set; }
        }

        private sealed class HttpParameterInput
        {
            public int targetid { get; set; }
        }

        private sealed class AnotherHttpParameterInput
        {
            public XElement data { get; set; }
        }

        private sealed class JsonToXmlConverter : IFormattedInputConverter<JObject, XElement>
        {
            public XElement Convert(JObject source) => JsonConvert.DeserializeXNode(source.ToString()).Root;
        }

        private sealed class FormattedInputBinder : IFormattedInputBinder<HttpBody, HttpParameterInput>
        {
            public void Bind(HttpBody source, HttpParameterInput target) => target.targetid = source.SourceId;
        }
    }
}
