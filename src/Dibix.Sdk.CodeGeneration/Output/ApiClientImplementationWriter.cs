﻿using System;
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
            context.AddUsing<HttpClient>();

            string className = $"{controller.Name}Service";
            string interfaceName = $"I{className}";
            CSharpAnnotation interfaceDescriptor = new CSharpAnnotation("HttpService", new CSharpValue($"typeof({interfaceName})"));
            CSharpClass @class = output.AddClass(className, CSharpModifiers.Public | CSharpModifiers.Sealed, interfaceDescriptor)
                                       .Implements(interfaceName);

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
            @class.AddField("_httpClientOptions", "HttpClientOptions", modifiers: CSharpModifiers.Private | CSharpModifiers.ReadOnly);

            if (requiresAuthorization)
                @class.AddField("_httpAuthorizationProvider", "IHttpAuthorizationProvider", modifiers: CSharpModifiers.Private | CSharpModifiers.ReadOnly);

            @class.AddSeparator();

            AddCtorWithoutClientOptions(@class, requiresAuthorization);
            AddPrimaryCtor(@class, requiresAuthorization);

            @class.AddSeparator();

            IList<ActionDefinition> actions = controller.Actions.OrderBy(x => operationIdMap[x]).ToArray();
            for (int i = 0; i < actions.Count; i++)
            {
                ActionDefinition action = actions[i];
                string body = GenerateMethodBody(controller, action, context, securitySchemeMap);
                AddMethod(action, context, operationIdMap, (methodName, returnType) => @class.AddMethod(methodName, returnType, body, modifiers: CSharpModifiers.Public | CSharpModifiers.Async));

                if (i + 1 < actions.Count)
                    @class.AddSeparator();
            }
        }
        #endregion

        #region Private Methods
        private static void AddPrimaryCtor(CSharpClass @class, bool requiresAuthorization)
        {
            StringBuilder ctorBodySb = new StringBuilder();
            ctorBodySb.AppendLine("_httpClientFactory = httpClientFactory;")
                      .Append("_httpClientOptions = httpClientOptions;");

            if (requiresAuthorization)
            {
                ctorBodySb.AppendLine()
                          .Append("_httpAuthorizationProvider = httpAuthorizationProvider;");
            }

            ctorBodySb.AppendLine()
                      .Append("_httpClientName = httpClientName;");

            string ctorBody = ctorBodySb.ToString();
            CSharpConstructor ctor = @class.AddConstructor(ctorBody)
                                           .AddParameter("httpClientFactory", "IHttpClientFactory")
                                           .AddParameter("httpClientOptions", "HttpClientOptions");

            if (requiresAuthorization)
                ctor.AddParameter("httpAuthorizationProvider", "IHttpAuthorizationProvider");

            ctor.AddParameter("httpClientName", "string");
        }

        private static void AddCtorWithoutClientOptions(CSharpClass @class, bool requiresAuthorization)
        {
            CSharpConstructor ctor = @class.AddConstructor()
                                           .AddParameter("httpClientFactory", "IHttpClientFactory");

            if (requiresAuthorization)
                ctor.AddParameter("httpAuthorizationProvider", "IHttpAuthorizationProvider");

            ctor.AddParameter("httpClientName", "string");

            ICSharpConstructorInvocationExpression constructorThisCall = ctor.CallThis()
                                                                             .AddParameter(new CSharpValue("httpClientFactory"))
                                                                             .AddParameter(new CSharpValue("HttpClientOptions.Default"));

            if (requiresAuthorization)
                constructorThisCall.AddParameter(new CSharpValue("httpAuthorizationProvider"));

            constructorThisCall.AddParameter(new CSharpValue("httpClientName"));
        }

        private string GenerateMethodBody(ControllerDefinition controller, ActionDefinition action, CodeGenerationContext context, IDictionary<string, SecurityScheme> securitySchemeMap)
        {
            bool CollectParameter(ActionParameter parameter)
            {
                // We don't support out parameters in REST APIs, but this accessor could still be used directly within the backend
                // Therefore we discard this parameter
                if (parameter.IsOutput)
                    return false;

                return true;
            }

            ICollection<ActionParameter> distinctParameters = action.Parameters
                                                                    .Where(CollectParameter)
                                                                    .DistinctBy(x => x.ApiParameterName)
                                                                    .ToArray();

            StringWriter writer = new StringWriter();
            writer.WriteLine($"using ({nameof(HttpClient)} client = _httpClientFactory.CreateClient(_httpClientName))")
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

                foreach (ActionParameter parameter in queryParameters)
                {
                    writer.Write($".AddQueryParam(nameof({parameter.ApiParameterName}), {parameter.ApiParameterName}");

                    if (parameter.DefaultValue != null)
                    {
                        string defaultValue = context.BuildDefaultValueLiteral(parameter.DefaultValue).AsString();
                        writer.WriteRaw($", {defaultValue}");
                    }

                    writer.WriteLineRaw(")");
                }

                writer.WriteLine(".Build();")
                      .ResetTemporaryIndent();

                uriConstant = "uri";
            }

            writer.WriteLine($"{nameof(HttpRequestMessage)} requestMessage = new {nameof(HttpRequestMessage)}(new {nameof(HttpMethod)}(\"{action.Method.ToString().ToUpperInvariant()}\"), {uriConstant});");

            bool oneOf = action.SecuritySchemes.Operator == SecuritySchemeOperator.Or && action.SecuritySchemes.Requirements.Count > 1;
            bool multipleOneOf = false;
            ICollection<SecuritySchemeRequirement> securitySchemeRequirements = action.SecuritySchemes
                                                                                      .Requirements
                                                                                      .Where(x => x.Scheme != SecuritySchemes.Anonymous)
                                                                                      .ToArray();

            foreach (SecuritySchemeRequirement securitySchemeRequirement in securitySchemeRequirements)
            {
                string securitySchemeName = securitySchemeRequirement.Scheme.SchemeName;
                string getAuthorizationValueCall = $"_httpAuthorizationProvider.GetValue(\"{securitySchemeName}\")";
                if (oneOf)
                {
                    writer.WriteIndent();

                    if (multipleOneOf)
                        writer.WriteRaw("else ");

                    writer.WriteLineRaw($"if ({getAuthorizationValueCall} != null)")
                          .PushIndent();

                    multipleOneOf = true;
                }

                SecurityScheme securityScheme = securitySchemeMap[securitySchemeName];
                (string authorizationHeaderName, string authorizationHeaderValue) = ResolveSecuritySchemeHeaderValues(securityScheme.Value, getAuthorizationValueCall);
                writer.WriteLine($"requestMessage.{nameof(HttpRequestMessage.Headers)}.{nameof(HttpRequestMessage.Headers.Add)}(\"{authorizationHeaderName}\", {authorizationHeaderValue});");

                if (oneOf)
                    writer.PopIndent();
            }

            if (oneOf && action.SecuritySchemes.Requirements.All(x => x.Scheme != SecuritySchemes.Anonymous))
            {
                writer.WriteLine("else")
                      .PushIndent()
                      .WriteLine($"throw new InvalidOperationException(\"None of the security scheme requirements were met:{String.Join("", securitySchemeRequirements.Select(x => $"{Environment.NewLine.Replace("\n", "\\n").Replace("\r", "\\r")}- {x.Scheme.SchemeName}"))}\");")
                      .PopIndent();
            }

            foreach (ActionParameter parameter in distinctParameters.Where(x => x.ParameterLocation == ActionParameterLocation.Header))
            {
                // Will be handled by SecurityScheme/IHttpAuthorizationProvider
                if (parameter.ApiParameterName == "Authorization" || action.SecuritySchemes.Requirements.Any(x => x.Scheme.SchemeName == parameter.ApiParameterName))
                    continue;

                string normalizedApiParameterName = context.NormalizeApiParameterName(parameter.ApiParameterName);
                if (!parameter.IsRequired)
                {
                    writer.WriteLine($"if ({normalizedApiParameterName} != null)")
                          .SetTemporaryIndent(4);
                }

                string headerValue = normalizedApiParameterName;
                if (parameter.Type is not PrimitiveTypeReference { Type: PrimitiveType.String })
                    headerValue = $"{headerValue}.ToString()";

                writer.WriteLine($"requestMessage.{nameof(HttpRequestMessage.Headers)}.{nameof(HttpRequestMessage.Headers.Add)}(\"{parameter.ApiParameterName}\", {headerValue});");

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
                    writer.WriteRaw($"ReadAsAsync<{responseContentType}>(MediaTypeFormattersFactory.Create(_httpClientOptions, client), cancellationToken)");
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

        private static (string name, string value) ResolveSecuritySchemeHeaderValues(SecuritySchemeValue securitySchemeValue, string credential) => securitySchemeValue switch
        {
            AuthorizationHeaderSecuritySchemeValue authorizationHeaderSecuritySchemeValue => (name: nameof(HttpRequestHeaders.Authorization), value: $"$\"{authorizationHeaderSecuritySchemeValue.Scheme} {{{credential}}}\""),
            HeaderSecuritySchemeValue headerSecuritySchemeValue => (name: headerSecuritySchemeValue.HeaderName, value: credential),
            _ => throw new ArgumentOutOfRangeException(nameof(securitySchemeValue), securitySchemeValue, null)
        };
        #endregion
    }
}