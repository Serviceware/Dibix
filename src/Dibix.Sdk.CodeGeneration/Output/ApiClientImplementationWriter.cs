using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Dibix.Http;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ApiClientImplementationWriter : ApiClientWriter
    {
        #region Properties
        public override string RegionName => "Implementation";
        #endregion

        #region Overrides
        protected override void WriteController(CodeGenerationContext context, CSharpStatementScope output, ControllerDefinition controller, string serviceName, IDictionary<ActionDefinition, string> operationIdMap, IDictionary<string, SecurityScheme> securitySchemeMap)
        {
            context.AddReference<HttpClient>();

            if (context.Model.UseMicrosoftHttpClient)
                context.AddUsing("IHttpClientFactory = System.Net.Http.IHttpClientFactory");

            string className = $"{controller.Name}Service";
            string interfaceName = $"I{className}";
            CSharpAnnotation interfaceDescriptor = new CSharpAnnotation("HttpService", new CSharpValue($"typeof({interfaceName})"));
            CSharpClass @class = output.AddClass(className, CSharpModifiers.Public | CSharpModifiers.Sealed, interfaceDescriptor)
                                       .Implements(interfaceName);

            if (!context.Model.UseMicrosoftHttpClient)
                @class.AddField("BaseAddress", nameof(Uri), new CSharpValue($"new {nameof(Uri)}(\"{context.Model.BaseUrl.TrimEnd('/')}/\")"), CSharpModifiers.Private | CSharpModifiers.Static | CSharpModifiers.ReadOnly);

            bool hasBodyParameter = controller.Actions.Any(x => x.RequestBody != null);
            bool requiresAuthorization = controller.Actions.Any(x => x.SecuritySchemes.HasEffectiveRequirements);

            if (hasBodyParameter)
            {
                //context.AddReference<MediaTypeFormatter>();
                context.AddUsing("System.Net.Http.Formatting");
                @class.AddField("Formatter", "MediaTypeFormatter", new CSharpValue("new JsonMediaTypeFormatter()"), CSharpModifiers.Private | CSharpModifiers.Static | CSharpModifiers.ReadOnly);
            }

            @class.AddField("_httpClientName", "string", modifiers: CSharpModifiers.Private | CSharpModifiers.ReadOnly);
            @class.AddField("_httpClientFactory", "IHttpClientFactory", modifiers: CSharpModifiers.Private | CSharpModifiers.ReadOnly);

            if (requiresAuthorization)
                @class.AddField("_httpAuthorizationProvider", "IHttpAuthorizationProvider", modifiers: CSharpModifiers.Private | CSharpModifiers.ReadOnly);

            @class.AddSeparator();

            if (!context.Model.UseMicrosoftHttpClient)
                AddCtorWithoutClientName(context, @class, requiresAuthorization);

            AddPrimaryCtor(@class, requiresAuthorization);

            @class.AddSeparator();

            IList<ActionDefinition> actions = controller.Actions.OrderBy(x => operationIdMap[x]).ToArray();
            for (int i = 0; i < actions.Count; i++)
            {
                ActionDefinition action = actions[i];
                string body = GenerateMethodBody(controller, action, context, securitySchemeMap);
                base.AddMethod(action, context, operationIdMap, (methodName, returnType) => @class.AddMethod(methodName, returnType, body, modifiers: CSharpModifiers.Public | CSharpModifiers.Async));

                if (i + 1 < actions.Count)
                    @class.AddSeparator();
            }
        }
        #endregion

        #region Private Methods
        private static void AddPrimaryCtor(CSharpClass @class, bool requiresAuthorization)
        {
            StringBuilder ctorBodySb = new StringBuilder("_httpClientFactory = httpClientFactory;");

            if (requiresAuthorization)
            {
                ctorBodySb.AppendLine()
                          .Append("_httpAuthorizationProvider = httpAuthorizationProvider;");
            }

            ctorBodySb.AppendLine()
                      .Append("_httpClientName = httpClientName;");

            string ctorBody = ctorBodySb.ToString();
            CSharpConstructor ctor = @class.AddConstructor(ctorBody)
                                           .AddParameter("httpClientFactory", "IHttpClientFactory");

            if (requiresAuthorization)
                ctor.AddParameter("httpAuthorizationProvider", "IHttpAuthorizationProvider");

            ctor.AddParameter("httpClientName", "string");
        }

        private static void AddCtorWithoutClientName(CodeGenerationContext context, CSharpClass @class, bool requiresAuthorization)
        {
            CSharpConstructor ctor = @class.AddConstructor()
                                           .AddParameter("httpClientFactory", "IHttpClientFactory");

            if (requiresAuthorization)
                ctor.AddParameter("httpAuthorizationProvider", "IHttpAuthorizationProvider");

            ICSharpConstructorInvocationExpression constructorThisCall = ctor.CallThis()
                                                                             .AddParameter(new CSharpValue("httpClientFactory"));

            if (requiresAuthorization)
                constructorThisCall.AddParameter(new CSharpValue("httpAuthorizationProvider"));

            string defaultClientNameValue = "DefaultHttpClientFactory.DefaultClientName";

            if (context.Model.UseMicrosoftHttpClient)
            {
                defaultClientNameValue = "Options.DefaultName";
                context.AddUsing("Microsoft.Extensions.Options");
            }

            constructorThisCall.AddParameter(new CSharpValue(defaultClientNameValue));
        }

        private string GenerateMethodBody(ControllerDefinition controller, ActionDefinition action, CodeGenerationContext context, IDictionary<string, SecurityScheme> securitySchemeMap)
        {
            ICollection<ActionParameter> distinctParameters = action.Parameters
                                                                    .DistinctBy(x => x.ApiParameterName)
                                                                    .ToArray();

            StringWriter writer = new StringWriter();
            writer.Write($"using ({nameof(HttpClient)} client = _httpClientFactory.CreateClient(_httpClientName");

            if (!context.Model.UseMicrosoftHttpClient)
                writer.Write(", BaseAddress");

            writer.WriteLine("))")
                  .WriteLine("{")
                  .PushIndent();

            string uri = RouteBuilder.BuildRoute(context.Model.AreaName, controller.Name, action.ChildRoute);
            string uriConstant = $"\"{uri}\"";
            if (uriConstant.Contains('{'))
                uriConstant = $"${uriConstant}";

            ICollection<ActionParameter> queryParameters = distinctParameters.Where(x => x.ParameterLocation == ActionParameterLocation.Query)
                                                                             .ToArray();

            if (queryParameters.Any())
            {
                context.AddUsing("UriBuilder = Dibix.Http.Client.UriBuilder");

                writer.WriteLine($"{nameof(Uri)} uri = UriBuilder.Create({uriConstant}, {nameof(UriKind)}.{nameof(UriKind.Relative)})")
                      .SetTemporaryIndent(20);

                foreach (ActionParameter parameter in distinctParameters.Where(x => x.ParameterLocation == ActionParameterLocation.Query))
                {
                    writer.WriteLine($".AddQueryParam(nameof({parameter.ApiParameterName}), {parameter.ApiParameterName})");
                }

                writer.WriteLine(".Build();")
                      .ResetTemporaryIndent();

                uriConstant = "uri";
            }

            writer.WriteLine($"{nameof(HttpRequestMessage)} requestMessage = new {nameof(HttpRequestMessage)}(new {nameof(HttpMethod)}(\"{action.Method.ToString().ToUpperInvariant()}\"), {uriConstant});");

            bool oneOf = action.SecuritySchemes.Operator == SecuritySchemeOperator.Or && action.SecuritySchemes.Requirements.Count > 1;
            foreach (SecuritySchemeRequirement securitySchemeRequirement in action.SecuritySchemes.Requirements.Where(x => x.Scheme != SecuritySchemes.Anonymous))
            {
                string securitySchemeName = securitySchemeRequirement.Scheme.Name;
                string getAuthorizationValueCall = $"_httpAuthorizationProvider.GetValue(\"{securitySchemeName}\")";
                if (oneOf)
                {
                    writer.WriteLine($"if ({getAuthorizationValueCall} != null)")
                          .PushIndent();
                }

                SecurityScheme securityScheme = securitySchemeMap[securitySchemeName];
                (string authorizationHeaderName, string authorizationHeaderValue) = ResolveSecuritySchemeHeaderValues(securitySchemeName, getAuthorizationValueCall, securityScheme.Kind);
                writer.WriteLine($"requestMessage.{nameof(HttpRequestMessage.Headers)}.{nameof(HttpRequestMessage.Headers.Add)}(\"{authorizationHeaderName}\", {authorizationHeaderValue});");

                if (oneOf)
                    writer.PopIndent();
            }

            foreach (ActionParameter parameter in distinctParameters.Where(x => x.ParameterLocation == ActionParameterLocation.Header))
            {
                // Will be handled by SecurityScheme/IHttpAuthorizationProvider
                if (parameter.ApiParameterName == "Authorization" || action.SecuritySchemes.Requirements.Any(x => x.Scheme.Name == parameter.ApiParameterName))
                    continue;

                string normalizedApiParameterName = NormalizeApiParameterName(parameter.ApiParameterName);
                if (!parameter.IsRequired)
                {
                    writer.WriteLine($"if ({normalizedApiParameterName} != null)")
                          .SetTemporaryIndent(4);
                }

                writer.WriteLine($"requestMessage.{nameof(HttpRequestMessage.Headers)}.{nameof(HttpRequestMessage.Headers.Add)}(\"{parameter.ApiParameterName}\", {normalizedApiParameterName});");
                
                if (!parameter.IsRequired)
                    writer.ResetTemporaryIndent();
            }

            if (action.RequestBody != null)
            {
                writer.Write($"requestMessage.{nameof(HttpRequestMessage.Content)} = new ");

                TypeReference requestBodyType = action.RequestBody.Contract;
                if (base.IsStream(requestBodyType))
                    writer.WriteRaw($"{nameof(StreamContent)}(body");
                else
                {
                    //context.AddReference<ObjectContent<object>>();
                    writer.WriteRaw($"ObjectContent<{context.ResolveTypeName(requestBodyType)}>(body, Formatter");
                }

                writer.WriteLineRaw(");");
            }

            writer.WriteLine($"{nameof(HttpResponseMessage)} responseMessage = await client.{nameof(HttpClient.SendAsync)}(requestMessage, cancellationToken).{nameof(Task.ConfigureAwait)}(false);");

            string responseContentType = null;
            if (action.DefaultResponseType != null)
                responseContentType = context.ResolveTypeName(action.DefaultResponseType, EnumerableBehavior.Collection);

            if (responseContentType != null)
            {
                writer.Write($"{responseContentType} responseContent = await responseMessage.{nameof(HttpResponseMessage.Content)}.");

                if (base.IsStream(action.DefaultResponseType))
                    writer.WriteRaw($"{nameof(HttpContent.ReadAsStreamAsync)}()");
                else
                {
                    //context.AddReference<MediaTypeFormatter>();
                    writer.WriteRaw($"ReadAsAsync<{responseContentType}>(MediaTypeFormattersFactory.Create(client), cancellationToken)");
                }

                writer.WriteLineRaw($".{nameof(Task.ConfigureAwait)}(false);");
            }

            writer.Write("return ");

            if (responseContentType != null)
                writer.WriteRaw($"new HttpResponse<{responseContentType}>(responseMessage, responseContent)");
            else
                writer.WriteRaw("responseMessage");

            writer.WriteRaw(";")
                  .WriteLine();

            writer.PopIndent()
                  .Write('}');

            string body = writer.ToString();
            return body;
        }

        private static (string name, string value) ResolveSecuritySchemeHeaderValues(string name, string value, SecuritySchemeKind kind)
        {
            switch (kind)
            {
                case SecuritySchemeKind.Bearer: return (name: nameof(HttpRequestHeaders.Authorization), value: $"$\"Bearer {{{value}}}\"");
                case SecuritySchemeKind.ApiKey: return (name: name, value: value);
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
        #endregion
    }
}