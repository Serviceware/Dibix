using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ApiClientImplementationWriter : ApiClientWriter
    {
        #region Properties
        public override string RegionName => "Implementation";
        #endregion

        #region Overrides
        protected override void WriteController(CodeGenerationContext context, ControllerDefinition controller, string serviceName, IDictionary<ActionDefinition, string> operationIdMap, IDictionary<string, SecurityScheme> securitySchemeMap)
        {
            context.AddReference<HttpClient>();

            string className = $"{controller.Name}Service";
            string interfaceName = $"I{className}";
            CSharpAnnotation interfaceDescriptor = new CSharpAnnotation("HttpService", new CSharpValue($"typeof({interfaceName})"));
            CSharpClass @class = context.Output
                                        .AddClass(className, CSharpModifiers.Public | CSharpModifiers.Sealed, interfaceDescriptor)
                                        .Implements(interfaceName)
                                        .AddField("BaseAddress", nameof(Uri), new CSharpValue($"new {nameof(Uri)}(\"{context.Model.BaseUrl.TrimEnd('/')}/\")"), CSharpModifiers.Private | CSharpModifiers.Static | CSharpModifiers.ReadOnly);

            bool hasBodyParameter = controller.Actions.Any(x => x.RequestBody != null);
            if (hasBodyParameter)
            {
                context.AddReference<MediaTypeFormatter>();
                @class.AddField("Formatter", nameof(MediaTypeFormatter), new CSharpValue($"new {nameof(JsonMediaTypeFormatter)}()"), CSharpModifiers.Private | CSharpModifiers.Static | CSharpModifiers.ReadOnly);
            }

            @class.AddField("_httpClientFactory", "IHttpClientFactory", modifiers: CSharpModifiers.Private | CSharpModifiers.ReadOnly)
                  .AddField("_authorizationProvider", "IHttpAuthorizationProvider", modifiers: CSharpModifiers.Private | CSharpModifiers.ReadOnly);

            @class.AddSeparator();

            bool requiresAuthorization = controller.Actions
                                                   .SelectMany(x => x.SecuritySchemes)
                                                   .SelectMany(x => x)
                                                   .Any(x => x != SecuritySchemes.Anonymous.Name);

            CSharpConstructor ctor1 = @class.AddConstructor();

            if (requiresAuthorization)
                ctor1.AddParameter("authorizationProvider", "IHttpAuthorizationProvider");

            ICSharpConstructorInvocationExpression constructorBaseCall = ctor1.CallThis()
                                                                              .AddParameter(new CSharpValue("new DefaultHttpClientFactory()"));

            if (requiresAuthorization)
                constructorBaseCall.AddParameter(new CSharpValue("authorizationProvider"));

            StringBuilder ctorBodySb = new StringBuilder("this._httpClientFactory = httpClientFactory;");

            if (requiresAuthorization)
            {
                ctorBodySb.AppendLine()
                          .Append("this._authorizationProvider = authorizationProvider;");
            }

            string ctorBody = ctorBodySb.ToString();
            CSharpConstructor ctor2 = @class.AddConstructor(ctorBody)
                                            .AddParameter("httpClientFactory", "IHttpClientFactory");

            if (requiresAuthorization)
                ctor2.AddParameter("authorizationProvider", "IHttpAuthorizationProvider");

            @class.AddSeparator();

            for (int i = 0; i < controller.Actions.Count; i++)
            {
                ActionDefinition action = controller.Actions[i];
                string body = this.GeneratedMethodBody(controller, action, context, securitySchemeMap);
                base.AddMethod(action, context, operationIdMap, (methodName, returnType) => @class.AddMethod(methodName, returnType, body, modifiers: CSharpModifiers.Public | CSharpModifiers.Async));

                if (i + 1 < controller.Actions.Count)
                    @class.AddSeparator();
            }
        }
        #endregion

        #region Private Methods
        private string GeneratedMethodBody(ControllerDefinition controller, ActionDefinition action, CodeGenerationContext context, IDictionary<string, SecurityScheme> securitySchemeMap)
        {
            ICollection<ActionParameter> distinctParameters = action.Parameters
                                                                    .DistinctBy(x => x.ApiParameterName)
                                                                    .ToArray();

            StringWriter writer = new StringWriter();
            writer.WriteLine($"using ({nameof(HttpClient)} client = this._httpClientFactory.CreateClient(BaseAddress))")
                  .WriteLine("{")
                  .PushIndent();

            string uri = RouteBuilder.BuildRoute(context.Model.AreaName, controller.Name, action.ChildRoute);
            string uriConstant = $"\"{uri}\"";
            if (uriConstant.Contains('{'))
                uriConstant = $"${uriConstant}";

            ICollection<ActionParameter> queryParameters = distinctParameters.Where(x => x.Location == ActionParameterLocation.Query)
                                                                             .ToArray();

            if (queryParameters.Any())
            {
                context.AddUsing("UriBuilder = Dibix.Http.Client.UriBuilder");

                writer.WriteLine($"{nameof(Uri)} uri = UriBuilder.Create({uriConstant}, {nameof(UriKind)}.{nameof(UriKind.Relative)})")
                      .SetTemporaryIndent(20);

                foreach (ActionParameter parameter in distinctParameters.Where(x => x.Location == ActionParameterLocation.Query))
                {
                    writer.WriteLine($".AddQueryParam(nameof({parameter.ApiParameterName}), {parameter.ApiParameterName})");
                }

                writer.WriteLine(".Build();")
                      .ResetTemporaryIndent();

                uriConstant = "uri";
            }

            writer.WriteLine($"{nameof(HttpRequestMessage)} requestMessage = new {nameof(HttpRequestMessage)}(new {nameof(HttpMethod)}(\"{action.Method.ToString().ToUpperInvariant()}\"), {uriConstant});");

            bool oneOf = action.SecuritySchemes.Count > 1;
            foreach (string securitySchemeName in action.SecuritySchemes.SelectMany(x => x).Where(x => x != SecuritySchemes.Anonymous.Name))
            {
                string getAuthorizationValueCall = $"this._authorizationProvider.GetValue(\"{securitySchemeName}\")";
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

            foreach (ActionParameter parameter in distinctParameters.Where(x => x.Location == ActionParameterLocation.Header))
            {
                // Will be handled by SecurityScheme/IHttpAuthorizationProvider
                if (parameter.ApiParameterName == "Authorization")
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
                    context.AddReference<ObjectContent<object>>();
                    writer.WriteRaw($"{nameof(ObjectContent<object>)}<{context.ResolveTypeName(requestBodyType, context)}>(body, Formatter");
                }

                writer.WriteLineRaw(");");
            }

            writer.WriteLine($"{nameof(HttpResponseMessage)} responseMessage = await client.{nameof(HttpClient.SendAsync)}(requestMessage, cancellationToken).{nameof(Task.ConfigureAwait)}(false);");

            string responseContentType = null;
            if (action.DefaultResponseType != null)
                responseContentType = context.ResolveTypeName(action.DefaultResponseType, context);

            if (responseContentType != null)
            {
                writer.Write($"{responseContentType} responseContent = await responseMessage.{nameof(HttpResponseMessage.Content)}.");

                if (base.IsStream(action.DefaultResponseType))
                    writer.WriteRaw($"{nameof(HttpContent.ReadAsStreamAsync)}()");
                else
                {
                    context.AddReference<MediaTypeFormatter>();
                    writer.WriteRaw($"{nameof(HttpContentExtensions.ReadAsAsync)}<{responseContentType}>(cancellationToken)");
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